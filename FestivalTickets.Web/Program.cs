using FestivalTickets.Domain;
using FestivalTickets.Domain.Discounts;
using FestivalTickets.Domain.Interfaces;
using FestivalTickets.Infrastructure;
using FestivalTickets.Infrastructure.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.IdleTimeout = TimeSpan.FromMinutes(30);
});

var connectionString = builder.Configuration.GetConnectionString("Default");
builder.Services.AddDbContext<ApplicationDbContext>(opt =>
    opt.UseSqlServer(connectionString, sql => sql.MigrationsAssembly("FestivalTickets.Infrastructure")));

builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 6;
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

builder.Services.AddScoped<IBookingRepository, BookingRepository>();
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<IFestivalRepository, FestivalRepository>();
builder.Services.AddScoped<IPackageRepository, PackageRepository>();
builder.Services.AddScoped<IItemRepository, ItemRepository>();
builder.Services.AddTransient<DiscountCalculator>();

var app = builder.Build();

await SeedIdentityAsync(app);

var culture = new CultureInfo("nl-NL");
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(culture),
    SupportedCultures = new List<CultureInfo> { culture },
    SupportedUICultures = new List<CultureInfo> { culture }
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

static async Task SeedIdentityAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    await EnsureRoleExistsAsync(roleManager, "Administrator");
    await EnsureRoleExistsAsync(roleManager, "Customer");

    await EnsureUserAsync(
        userManager,
        email: "admin@festivaltickets.nl",
        password: "Admin@123!",
        role: "Administrator",
        hasLoyaltyCard: false);

    var lisa = await EnsureUserAsync(
        userManager,
        email: "lisa@festivaltickets.nl",
        password: "Customer@123!",
        role: "Customer",
        hasLoyaltyCard: true);

    if (!await db.Customers.AnyAsync(c => c.UserId == lisa.Id))
    {
        db.Customers.Add(new Customer
        {
            FirstName = "Lisa",
            LastName = "Jansen",
            Email = "lisa@festivaltickets.nl",
            UserId = lisa.Id
        });
        await db.SaveChangesAsync();
    }

    var tom = await EnsureUserAsync(
        userManager,
        email: "tom@festivaltickets.nl",
        password: "Customer@123!",
        role: "Customer",
        hasLoyaltyCard: false);

    if (!await db.Customers.AnyAsync(c => c.UserId == tom.Id))
    {
        db.Customers.Add(new Customer
        {
            FirstName = "Tom",
            LastName = "Klant",
            Email = "tom@festivaltickets.nl",
            UserId = tom.Id
        });
        await db.SaveChangesAsync();
    }
}

static async Task EnsureRoleExistsAsync(RoleManager<IdentityRole> roleManager, string role)
{
    if (!await roleManager.RoleExistsAsync(role))
    {
        await roleManager.CreateAsync(new IdentityRole(role));
    }
}

static async Task<IdentityUser> EnsureUserAsync(
    UserManager<IdentityUser> userManager,
    string email,
    string password,
    string role,
    bool hasLoyaltyCard)
{
    var user = await userManager.FindByEmailAsync(email);
    if (user == null)
    {
        user = new IdentityUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true
        };

        var createResult = await userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
        {
            throw new InvalidOperationException(
                $"Seed user '{email}' failed: {string.Join(", ", createResult.Errors.Select(e => e.Description))}");
        }
    }

    if (!await userManager.IsInRoleAsync(user, role))
    {
        await userManager.AddToRoleAsync(user, role);
    }

    var claims = await userManager.GetClaimsAsync(user);
    var loyaltyClaim = claims.FirstOrDefault(c => c.Type == "LoyaltyCard");

    if (hasLoyaltyCard)
    {
        if (loyaltyClaim == null || loyaltyClaim.Value != "true")
        {
            if (loyaltyClaim != null)
            {
                await userManager.RemoveClaimAsync(user, loyaltyClaim);
            }

            await userManager.AddClaimAsync(user, new Claim("LoyaltyCard", "true"));
        }
    }
    else if (loyaltyClaim != null)
    {
        await userManager.RemoveClaimAsync(user, loyaltyClaim);
    }

    return user;
}
