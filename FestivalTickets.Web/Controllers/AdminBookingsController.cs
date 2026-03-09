using FestivalTickets.Domain.Interfaces;
using FestivalTickets.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FestivalTickets.Web.Controllers;

[Authorize(Roles = "Administrator")]
public class AdminBookingsController : Controller
{
    private readonly IBookingRepository _bookingRepo;
    private readonly ICustomerRepository _customerRepo;
    private readonly UserManager<IdentityUser> _userManager;

    public AdminBookingsController(
        IBookingRepository bookingRepo,
        ICustomerRepository customerRepo,
        UserManager<IdentityUser> userManager)
    {
        _bookingRepo = bookingRepo;
        _customerRepo = customerRepo;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var bookings = await _bookingRepo.GetAllAsync();
        return View(bookings);
    }

    public async Task<IActionResult> Details(int id)
    {
        var booking = await _bookingRepo.GetByIdAsync(id);
        if (booking == null)
        {
            return NotFound();
        }

        return View(booking);
    }

    public async Task<IActionResult> Delete(int id)
    {
        var booking = await _bookingRepo.GetByIdAsync(id);
        if (booking == null)
        {
            return NotFound();
        }

        return View(booking);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        await _bookingRepo.DeleteAsync(id);
        await _bookingRepo.SaveChangesAsync();

        TempData["Success"] = "Boeking verwijderd.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> ManageLoyalty()
    {
        var customers = await _customerRepo.GetAllAsync();
        var models = new List<LoyaltyViewModel>();

        foreach (var customer in customers)
        {
            var user = await _userManager.FindByIdAsync(customer.UserId);
            if (user == null)
            {
                continue;
            }

            var claims = await _userManager.GetClaimsAsync(user);
            models.Add(new LoyaltyViewModel
            {
                CustomerId = customer.Id,
                FullName = $"{customer.FirstName} {customer.LastName}",
                Email = customer.Email,
                HasLoyaltyCard = claims.Any(c => c.Type == "LoyaltyCard" && c.Value == "true")
            });
        }

        return View(models);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GrantLoyalty(int customerId)
    {
        var customer = await _customerRepo.GetByIdAsync(customerId);
        if (customer == null)
        {
            return NotFound();
        }

        var user = await _userManager.FindByIdAsync(customer.UserId);
        if (user == null)
        {
            return NotFound();
        }

        var claims = await _userManager.GetClaimsAsync(user);
        if (!claims.Any(c => c.Type == "LoyaltyCard"))
        {
            await _userManager.AddClaimAsync(user, new Claim("LoyaltyCard", "true"));
        }

        TempData["Success"] = $"Loyaliteitskaart toegekend aan {customer.FirstName}.";
        return RedirectToAction(nameof(ManageLoyalty));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RevokeLoyalty(int customerId)
    {
        var customer = await _customerRepo.GetByIdAsync(customerId);
        if (customer == null)
        {
            return NotFound();
        }

        var user = await _userManager.FindByIdAsync(customer.UserId);
        if (user == null)
        {
            return NotFound();
        }

        var claims = await _userManager.GetClaimsAsync(user);
        var loyaltyClaim = claims.FirstOrDefault(c => c.Type == "LoyaltyCard");
        if (loyaltyClaim != null)
        {
            await _userManager.RemoveClaimAsync(user, loyaltyClaim);
        }

        TempData["Success"] = $"Loyaliteitskaart ingetrokken van {customer.FirstName}.";
        return RedirectToAction(nameof(ManageLoyalty));
    }
}
