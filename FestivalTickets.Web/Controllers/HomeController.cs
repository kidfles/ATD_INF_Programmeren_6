using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using FestivalTickets.Web.Models;
using Microsoft.AspNetCore.Authorization;

namespace FestivalTickets.Web.Controllers;

// This is the default controller for the landing page.
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    // GET: /
    // Just shows the homepage.
    [AllowAnonymous]
    public IActionResult Index()
    {
        return View();
    }

    // Handles errors. If something breaks, this shows the "Oops" page.
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
