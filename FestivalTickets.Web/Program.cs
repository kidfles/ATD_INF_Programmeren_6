using FestivalTickets.Domain;
using FestivalTickets.Domain.Discounts;
using FestivalTickets.Domain.Interfaces;
using FestivalTickets.Infrastructure;
using FestivalTickets.Infrastructure.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Localization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.IdleTimeout = TimeSpan.FromMinutes(30);
});

var cs = builder.Configuration.GetConnectionString("Default");
builder.Services.AddDbContext<ApplicationDbContext>(opt =>
    opt.UseSqlServer(cs, sql => sql.MigrationsAssembly("FestivalTickets.Infrastructure")));
builder.Services.AddIdentity<IdentityUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

builder.Services.AddScoped<IBookingRepository, BookingRepository>();
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<IFestivalRepository, FestivalRepository>();
builder.Services.AddScoped<IPackageRepository, PackageRepository>();
builder.Services.AddScoped<IItemRepository, ItemRepository>();
builder.Services.AddScoped<DiscountCalculator>();

var app = builder.Build();
await SeedIdentityAsync(app);

// Tells the app to use Dutch formatting (like commas for decimals).
var cultureInfo = new CultureInfo("nl-NL");

app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(cultureInfo),
    SupportedCultures = new List<CultureInfo> { cultureInfo },
    SupportedUICultures = new List<CultureInfo> { cultureInfo }
});

// Configure the HTTP request pipeline.
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
