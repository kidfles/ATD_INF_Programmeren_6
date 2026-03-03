using FestivalTickets.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FestivalTickets.Web.Controllers;

[Authorize(Roles = "Customer")]
public class CustomerBookingsController : Controller
{
    private readonly IBookingRepository _bookingRepo;
    private readonly ICustomerRepository _customerRepo;
    private readonly UserManager<IdentityUser> _userManager;

    public CustomerBookingsController(
        IBookingRepository bookingRepo,
        ICustomerRepository customerRepo,
        UserManager<IdentityUser> userManager)
    {
        _bookingRepo = bookingRepo;
        _customerRepo = customerRepo;
        _userManager = userManager;
    }

    // GET: /CustomerBookings/Index
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Challenge();
        }

        var customer = await _customerRepo.GetByUserIdAsync(user.Id);
        if (customer == null)
        {
            return NotFound();
        }

        var bookings = await _bookingRepo.GetByCustomerIdAsync(customer.Id);
        return View(bookings);
    }

    // GET: /CustomerBookings/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Challenge();
        }

        var customer = await _customerRepo.GetByUserIdAsync(user.Id);
        if (customer == null)
        {
            return NotFound();
        }

        var booking = await _bookingRepo.GetByIdAsync(id);
        if (booking == null || booking.CustId != customer.Id)
        {
            return Forbid();
        }

        return View(booking);
    }
}
