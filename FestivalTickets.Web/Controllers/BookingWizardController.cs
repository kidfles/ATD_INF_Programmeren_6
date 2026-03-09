using FestivalTickets.Domain;
using FestivalTickets.Domain.Discounts;
using FestivalTickets.Domain.Interfaces;
using FestivalTickets.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FestivalTickets.Web.Controllers;

public class BookingWizardController : Controller
{
    private readonly IFestivalRepository _festivalRepo;
    private readonly IPackageRepository _packageRepo;
    private readonly IItemRepository _itemRepo;
    private readonly IBookingRepository _bookingRepo;
    private readonly ICustomerRepository _customerRepo;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly DiscountCalculator _discountCalc;

    public BookingWizardController(
        IFestivalRepository festivalRepo,
        IPackageRepository packageRepo,
        IItemRepository itemRepo,
        IBookingRepository bookingRepo,
        ICustomerRepository customerRepo,
        UserManager<IdentityUser> userManager,
        DiscountCalculator discountCalc)
    {
        _festivalRepo = festivalRepo;
        _packageRepo = packageRepo;
        _itemRepo = itemRepo;
        _bookingRepo = bookingRepo;
        _customerRepo = customerRepo;
        _userManager = userManager;
        _discountCalc = discountCalc;
    }

    // GET /BookingWizard/Step1
    public async Task<IActionResult> Step1(DateOnly? from, DateOnly? to)
    {
        var state = WizardSessionHelper.Load(HttpContext.Session);

        var today = DateOnly.FromDateTime(DateTime.Today);
        state.FilterFrom = from ?? (state.FilterFrom == default ? today : state.FilterFrom);
        state.FilterTo = to ?? (state.FilterTo == default ? today.AddYears(2) : state.FilterTo);

        if (state.FilterTo < state.FilterFrom)
        {
            state.FilterTo = state.FilterFrom;
        }

        WizardSessionHelper.Save(HttpContext.Session, state);

        var festivals = await _festivalRepo.GetUpcomingAsync(state.FilterFrom, state.FilterTo);
        var vm = new Step1ViewModel
        {
            Festivals = festivals.ToList(),
            FilterFrom = state.FilterFrom,
            FilterTo = state.FilterTo,
            SelectedFestivalId = state.SelectedFestivalId
        };

        return View(vm);
    }

    // POST /BookingWizard/Step1
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Step1(Step1ViewModel model)
    {
        if (model.SelectedFestivalId == null)
        {
            ModelState.AddModelError(string.Empty, "Selecteer een festival.");
            model.Festivals = (await _festivalRepo.GetUpcomingAsync(model.FilterFrom, model.FilterTo)).ToList();
            return View(model);
        }

        var state = WizardSessionHelper.Load(HttpContext.Session);
        state.FilterFrom = model.FilterFrom;
        state.FilterTo = model.FilterTo;
        state.SelectedFestivalId = model.SelectedFestivalId;
        state.SelectedPackageId = null;
        state.ExtraItems.Clear();
        state.BaseTotal = 0m;
        state.FinalTotal = 0m;
        state.Discounts.Clear();

        WizardSessionHelper.Save(HttpContext.Session, state);
        return RedirectToAction(nameof(Step2));
    }

    // GET /BookingWizard/Step2
    public async Task<IActionResult> Step2()
    {
        var state = WizardSessionHelper.Load(HttpContext.Session);
        if (state.SelectedFestivalId == null)
        {
            return RedirectToAction(nameof(Step1));
        }

        var festival = await _festivalRepo.GetByIdWithPackagesAsync(state.SelectedFestivalId.Value);
        if (festival == null)
        {
            return NotFound();
        }

        var vm = new Step2ViewModel
        {
            Festival = festival,
            Packages = festival.Packages.OrderBy(p => p.Name).ToList(),
            SelectedPackageId = state.SelectedPackageId,
            TicketQuantity = state.TicketQuantity
        };

        return View(vm);
    }

    // POST /BookingWizard/Step2
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Step2(Step2ViewModel model)
    {
        var state = WizardSessionHelper.Load(HttpContext.Session);
        if (state.SelectedFestivalId == null)
        {
            return RedirectToAction(nameof(Step1));
        }

        var festival = await _festivalRepo.GetByIdWithPackagesAsync(state.SelectedFestivalId.Value);
        if (festival == null)
        {
            return NotFound();
        }

        var packageExists = model.SelectedPackageId.HasValue && festival.Packages.Any(p => p.Id == model.SelectedPackageId.Value);
        if (!packageExists || model.TicketQuantity < 1)
        {
            ModelState.AddModelError(string.Empty, "Selecteer een ticket en voer een geldig aantal in.");
            model.Festival = festival;
            model.Packages = festival.Packages.OrderBy(p => p.Name).ToList();
            return View(model);
        }

        state.SelectedPackageId = model.SelectedPackageId;
        state.TicketQuantity = model.TicketQuantity;
        state.BaseTotal = 0m;
        state.FinalTotal = 0m;
        state.Discounts.Clear();

        WizardSessionHelper.Save(HttpContext.Session, state);
        return RedirectToAction(nameof(Step3));
    }

    // GET /BookingWizard/Step3
    public async Task<IActionResult> Step3()
    {
        var state = WizardSessionHelper.Load(HttpContext.Session);
        if (state.SelectedPackageId == null)
        {
            return RedirectToAction(nameof(Step2));
        }

        var package = await _packageRepo.GetByIdWithItemsAsync(state.SelectedPackageId.Value);
        if (package == null)
        {
            return RedirectToAction(nameof(Step2));
        }

        ViewBag.PackageName = package.Name;
        ViewBag.FestivalName = package.Festival?.Name;
        ViewBag.TicketQuantity = state.TicketQuantity;

        var allItems = await _itemRepo.GetAllAsync();
        var vm = new Step3ViewModel
        {
            AllItems = allItems.ToList(),
            ExtraItems = state.ExtraItems
        };

        return View(vm);
    }

    // POST /BookingWizard/Step3
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Step3(Step3ViewModel model)
    {
        var state = WizardSessionHelper.Load(HttpContext.Session);
        if (state.SelectedPackageId == null)
        {
            return RedirectToAction(nameof(Step2));
        }

        var package = await _packageRepo.GetByIdWithItemsAsync(state.SelectedPackageId.Value);
        if (package == null)
        {
            return RedirectToAction(nameof(Step2));
        }

        var allItems = (await _itemRepo.GetAllAsync()).ToDictionary(i => i.Id);

        state.ExtraItems.Clear();
        if (model.SelectedItemIds != null)
        {
            for (int i = 0; i < model.SelectedItemIds.Count; i++)
            {
                var itemId = model.SelectedItemIds[i];
                var qty = model.Quantities != null && i < model.Quantities.Count ? model.Quantities[i] : 0;

                if (qty < 1 || !allItems.TryGetValue(itemId, out var item))
                {
                    continue;
                }

                state.ExtraItems.Add(new WizardItemEntry
                {
                    ItemId = itemId,
                    ItemName = item.Name,
                    ItemType = item.ItemType,
                    UnitPrice = item.Price,
                    Quantity = qty
                });
            }
        }

        state.BaseTotal = 0m;
        state.FinalTotal = 0m;
        state.Discounts.Clear();
        WizardSessionHelper.Save(HttpContext.Session, state);

        return RedirectToAction(nameof(Step4));
    }

    // GET /BookingWizard/Step4
    public IActionResult Step4()
    {
        var state = WizardSessionHelper.Load(HttpContext.Session);
        if (state.SelectedPackageId == null)
        {
            return RedirectToAction(nameof(Step1));
        }

        if (User.IsInRole("Customer"))
        {
            return RedirectToAction(nameof(Step5));
        }

        if (User.IsInRole("Administrator"))
        {
            TempData["Error"] = "Beheerders kunnen geen tickets boeken.";
            return RedirectToAction("Index", "Home");
        }

        return View();
    }

    // GET /BookingWizard/Step5
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> Step5()
    {
        var state = WizardSessionHelper.Load(HttpContext.Session);
        if (state.SelectedPackageId == null)
        {
            return RedirectToAction(nameof(Step1));
        }

        var package = await _packageRepo.GetByIdWithItemsAsync(state.SelectedPackageId.Value);
        if (package == null || package.Festival == null)
        {
            return NotFound();
        }

        bool hasLoyalty = User.HasClaim("LoyaltyCard", "true");

        decimal packageItemsCost = package.PackageItems.Sum(pi => pi.Item.Price * pi.Quantity);
        decimal ticketUnitPrice = package.Festival.BasicPrice + packageItemsCost;
        decimal baseTotal = ticketUnitPrice * state.TicketQuantity
                            + state.ExtraItems.Sum(e => e.UnitPrice * e.Quantity);

        var context = new DiscountContext
        {
            BaseTotal = baseTotal,
            CurrentTotal = baseTotal,
            TicketQuantity = state.TicketQuantity,
            BookingDate = DateTime.Now,
            FestivalStartDate = package.Festival.StartDate,
            HasLoyaltyCard = hasLoyalty,
            ExtraItems = state.ExtraItems
                .Select(e => (e.ItemId, e.ItemType, e.UnitPrice, e.Quantity))
                .ToList()
        };

        var breakdown = _discountCalc.GetBreakdown(context);
        context.CurrentTotal = context.BaseTotal;
        decimal finalTotal = _discountCalc.Calculate(context);

        state.BaseTotal = baseTotal;
        state.FinalTotal = finalTotal;
        state.Discounts = breakdown
            .Select(d => new DiscountLine { Name = d.Name, Saving = d.Saving })
            .ToList();

        WizardSessionHelper.Save(HttpContext.Session, state);

        var vm = new Step5ViewModel
        {
            Package = package,
            TicketQuantity = state.TicketQuantity,
            ExtraItems = state.ExtraItems,
            BaseTotal = baseTotal,
            DiscountLines = state.Discounts,
            FinalTotal = finalTotal
        };

        return View(vm);
    }

    // POST /BookingWizard/Confirm
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> Confirm()
    {
        var state = WizardSessionHelper.Load(HttpContext.Session);
        if (state.SelectedPackageId == null)
        {
            return RedirectToAction(nameof(Step1));
        }

        if (state.FinalTotal <= 0m || state.TicketQuantity < 1)
        {
            return RedirectToAction(nameof(Step5));
        }

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

        var booking = new Booking
        {
            PackageId = state.SelectedPackageId.Value,
            Quantity = state.TicketQuantity,
            CustId = customer.Id,
            BookingDate = DateTime.Now,
            TotalPricePaid = state.FinalTotal,
            BookingItems = state.ExtraItems.Select(e => new BookingItem
            {
                ItemId = e.ItemId,
                Quantity = e.Quantity
            }).ToList()
        };

        await _bookingRepo.AddAsync(booking);
        await _bookingRepo.SaveChangesAsync();

        WizardSessionHelper.Clear(HttpContext.Session);
        TempData["Success"] = "Boeking bevestigd!";
        return RedirectToAction("Details", "CustomerBookings", new { id = booking.Id });
    }

    // POST /BookingWizard/Cancel
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Cancel()
    {
        WizardSessionHelper.Clear(HttpContext.Session);
        return RedirectToAction("Index", "Home");
    }
}
