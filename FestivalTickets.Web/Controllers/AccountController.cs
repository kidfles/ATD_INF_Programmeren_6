using FestivalTickets.Domain;
using FestivalTickets.Domain.Interfaces;
using FestivalTickets.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FestivalTickets.Web.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<IdentityUser>  _userManager;
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly ICustomerRepository _customerRepo;

    public AccountController(
        UserManager<IdentityUser> userManager,
        SignInManager<IdentityUser> signInManager,
        ICustomerRepository customerRepo)
    {
        _userManager   = userManager;
        _signInManager = signInManager;
        _customerRepo  = customerRepo;
    }

    // GET /Account/Login
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }

    // POST /Account/Login
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (!ModelState.IsValid) return View(model);

        var result = await _signInManager.PasswordSignInAsync(
            model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);

        if (result.Succeeded)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            // Redirect Administrator to bookings, Customer to welcome
            if (user != null && await _userManager.IsInRoleAsync(user, "Administrator"))
                return RedirectToAction("Index", "AdminBookings");

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Home");
        }

        ModelState.AddModelError(string.Empty, "Ongeldig e-mailadres of wachtwoord.");
        return View(model);
    }

    // GET /Account/Register
    [HttpGet]
    public IActionResult Register(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View(new RegisterViewModel());
    }

    // POST /Account/Register
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (!ModelState.IsValid) return View(model);

        var user = new IdentityUser { UserName = model.Email, Email = model.Email, EmailConfirmed = true };
        var result = await _userManager.CreateAsync(user, model.Password);

        if (result.Succeeded)
        {
            var roleResult = await _userManager.AddToRoleAsync(user, "Customer");
            if (!roleResult.Succeeded)
            {
                await _userManager.DeleteAsync(user);
                foreach (var error in roleResult.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
                return View(model);
            }

            var customer = new Customer
            {
                FirstName = model.FirstName,
                LastName  = model.LastName,
                Email     = model.Email,
                UserId    = user.Id
            };

            try
            {
                await _customerRepo.AddAsync(customer);
                await _customerRepo.SaveChangesAsync();

                await _signInManager.SignInAsync(user, isPersistent: false);

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);

                return RedirectToAction("Index", "Home");
            }
            catch (Exception)
            {
                // Rollback door de gebruiker te verwijderen
                await _userManager.DeleteAsync(user);
                ModelState.AddModelError(string.Empty, "Er is een fout opgetreden tijdens het opslaan van uw gegevens. Probeer het opnieuw.");
                return View(model);
            }
        }

        foreach (var error in result.Errors)
            ModelState.AddModelError(string.Empty, error.Description);

        return View(model);
    }

    // POST /Account/Logout
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    // GET /Account/AccessDenied
    public IActionResult AccessDenied() => View();

    // GET /Account/Profile
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> Profile()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        var customer = await _customerRepo.GetByUserIdAsync(user.Id);
        if (customer == null) return NotFound();

        return View(new ProfileViewModel
        {
            FirstName = customer.FirstName,
            LastName  = customer.LastName,
            Email     = user.Email ?? string.Empty
        });
    }

    // POST /Account/Profile
    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> Profile(ProfileViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        var customer = await _customerRepo.GetByUserIdAsync(user.Id);
        if (customer == null) return NotFound();

        customer.FirstName = model.FirstName;
        customer.LastName  = model.LastName;
        await _customerRepo.SaveChangesAsync();

        TempData["Success"] = "Profiel bijgewerkt.";
        return RedirectToAction(nameof(Profile));
    }
}
