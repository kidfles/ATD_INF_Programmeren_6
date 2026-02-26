using FestivalTickets.Domain.Discounts;
using FestivalTickets.Domain.Interfaces;
using FestivalTickets.Infrastructure;
using FestivalTickets.Infrastructure.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using Microsoft.AspNetCore.Localization;

var builder = WebApplication.CreateBuilder(args);

// ── MVC + Session ────────────────────────────────────────────────────
builder.Services.AddControllersWithViews();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// ── Database ──────────────────────────────────────────────────────────
var cs = builder.Configuration.GetConnectionString("Default");
builder.Services.AddDbContext<ApplicationDbContext>(opt =>
    opt.UseSqlServer(cs, sql => sql.MigrationsAssembly("FestivalTickets.Infrastructure")));

// ── Identity ──────────────────────────────────────────────────────────
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.Password.RequireDigit           = true;
    options.Password.RequireLowercase       = true;
    options.Password.RequireUppercase       = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength         = 6;
    options.SignIn.RequireConfirmedAccount  = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// ── Cookie paths ──────────────────────────────────────────────────────
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath    = "/Account/Login";
    options.LogoutPath   = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

// ── Repository DI ─────────────────────────────────────────────────────
builder.Services.AddScoped<IBookingRepository,  BookingRepository>();
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<IFestivalRepository, FestivalRepository>();
builder.Services.AddScoped<IPackageRepository,  PackageRepository>();
builder.Services.AddScoped<IItemRepository,     ItemRepository>();

// ── Discount Calculator (transient — new instance each time) ──────────
builder.Services.AddTransient<DiscountCalculator>();

var app = builder.Build();

// ── Dutch locale ──────────────────────────────────────────────────────
var cultureInfo = new CultureInfo("nl-NL");
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(cultureInfo),
    SupportedCultures    = new List<CultureInfo> { cultureInfo },
    SupportedUICultures  = new List<CultureInfo> { cultureInfo }
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();        // ← MUST come after UseRouting and before authentication/authorization and MapControllerRoute
app.UseAuthentication(); // ← MUST come before UseAuthorization
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// ── Seed Identity roles + users ───────────────────────────────────────
await SeedIdentityAsync(app);

app.Run();

// ─────────────────────────────────────────────────────────────────────
static async Task SeedIdentityAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
    var db          = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    // Ensure roles exist
    foreach (var role in new[] { "Administrator", "Customer" })
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }

    // Admin account
    await EnsureUserAsync(userManager, db,
        email: "admin@festivaltickets.nl",
        password: "Admin@123!",
        role: "Administrator",
        firstName: "Admin",
        lastName: "User",
        loyaltyCard: false);

    // Customer with loyalty card
    await EnsureUserAsync(userManager, db,
        email: "lisa@festivaltickets.nl",
        password: "Customer@123!",
        role: "Customer",
        firstName: "Lisa",
        lastName: "Janssen",
        loyaltyCard: true);

    // Customer without loyalty card
    await EnsureUserAsync(userManager, db,
        email: "tom@festivaltickets.nl",
        password: "Customer@123!",
        role: "Customer",
        firstName: "Tom",
        lastName: "de Vries",
        loyaltyCard: false);
}

static async Task EnsureUserAsync(
    UserManager<IdentityUser> userManager,
    ApplicationDbContext db,
    string email, string password, string role,
    string firstName, string lastName, bool loyaltyCard)
{
    var user = await userManager.FindByEmailAsync(email);
    if (user == null)
    {
        user = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
            throw new Exception($"Failed to seed user {email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
    }

    if (!await userManager.IsInRoleAsync(user, role))
        await userManager.AddToRoleAsync(user, role);

    if (loyaltyCard)
    {
        var claims = await userManager.GetClaimsAsync(user);
        if (!claims.Any(c => c.Type == "LoyaltyCard"))
            await userManager.AddClaimAsync(user, new System.Security.Claims.Claim("LoyaltyCard", "true"));
    }

    // Create Customer record (for Customer role only)
    if (role == "Customer" && !db.Customers.Any(c => c.UserId == user.Id))
    {
        db.Customers.Add(new FestivalTickets.Domain.Customer
        {
            FirstName = firstName,
            LastName  = lastName,
            Email     = email,
            UserId    = user.Id
        });
        await db.SaveChangesAsync();
    }
}
