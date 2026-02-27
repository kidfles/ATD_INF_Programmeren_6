using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FestivalTickets.Web.Controllers;

[Authorize(Roles = "Administrator")]
public sealed class AdminBookingsController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
