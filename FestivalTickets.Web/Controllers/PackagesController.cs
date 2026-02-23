using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using FestivalTickets.Domain;
using FestivalTickets.Infrastructure;
using FestivalTickets.Web.Models;

namespace FestivalTickets.Web.Controllers
{
    // This is the main controller that handles everything related to Packages.
    public class PackagesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PackagesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Packages
        // Shows the big list of packages. You can filter them by clicking a festival in the dropdown.
        public async Task<IActionResult> Index(int? festivalId)
        {
            ViewData["FestivalId"] = festivalId;

            // Fills up the dropdown list so you can pick a festival.
            ViewData["AllFestivals"] = new SelectList(await _context.Festivals.OrderBy(f => f.Name).ToListAsync(), "Id", "Name", festivalId);

            // If you picked a festival, we grab its name to show it off.
            if (festivalId.HasValue)
            {
                var festivalName = await _context.Festivals
                    .Where(f => f.Id == festivalId.Value)
                    .Select(f => f.Name)
                    .FirstOrDefaultAsync();
                ViewData["FestivalName"] = festivalName;
            }

            IQueryable<Package> query = _context.Packages.Include(p => p.Festival);
            // Checking if we need to filter the list.
            if (festivalId.HasValue)
            {
                query = query.Where(p => p.FestivalId == festivalId.Value);
            }
            return View(await query.AsNoTracking().ToListAsync());
        }

        // GET: Packages/Details/5
        // Grabs all the nitty-gritty details for a single package.
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Fetch the package with all its related data (festival, items involved).
            var package = await _context.Packages
                .Include(p => p.Festival)
                .Include(p => p.PackageItems)
                    .ThenInclude(pi => pi.Item)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);
            if (package == null)
            {
                return NotFound();
            }

            // Organize the items nicely by type and name.
            var items = package.PackageItems
                .OrderBy(pi => pi.Item.ItemType)
                .ThenBy(pi => pi.Item.Name)
                .Select(pi => new PackageDetailsItemViewModel
                {
                    ItemName = pi.Item.Name,
                    ItemType = pi.Item.ItemType,
                    Quantity = pi.Quantity,
                    UnitPrice = pi.Item.Price
                })
                .ToList();

            var vm = new PackageDetailsViewModel
            {
                Id = package.Id,
                Name = package.Name,
                FestivalId = package.FestivalId,
                FestivalName = package.Festival!.Name,
                FestivalPlace = package.Festival!.Place,
                Items = items,
                TotalPrice = items.Sum(i => i.LineTotal)
            };

            return View(vm);
        }

        // GET: Packages/Create
        // Sets up the form to create a brand new package.
        public async Task<IActionResult> Create(int? festivalId)
        {
            await PopulateFestivalSelectListAsync(festivalId);
            return View(new Package { FestivalId = festivalId ?? 0 });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        // This actually saves the new package to the database.
        public async Task<IActionResult> Create([Bind("Id,FestivalId,Name")] Package package)
        { 
            // Double check that the festival actually exists.
            if (!await _context.Festivals.AnyAsync(f => f.Id == package.FestivalId))
            {
                ModelState.AddModelError(nameof(Package.FestivalId), "Selecteer een bestaand festival.");
            }

            // If something's wrong, we send them back to the drawing board (the form).
            if (!ModelState.IsValid)
            {
                await PopulateFestivalSelectListAsync(package.FestivalId);
                return View(package);
            }

            // Save it!
            _context.Add(package);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index),
                new { festivalId = package.FestivalId });
        }


        // GET: Packages/Edit/5
        // Opens the form to edit an existing package.
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var package = await _context.Packages.FindAsync(id);
            if (package == null)
            {
                return NotFound();
            }
            await PopulateFestivalSelectListAsync(package.FestivalId);
            return View(package);
        }

        // POST: Packages/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        // Saves the changes you made to the package.
        public async Task<IActionResult> Edit(int id, [Bind("Id,FestivalId,Name")] Package package)
        {
            if (id != package.Id)
            {
                return NotFound();
            }

            if (!await _context.Festivals.AnyAsync(f => f.Id == package.FestivalId))
            {
                ModelState.AddModelError(nameof(Package.FestivalId), "Selecteer een bestaand festival.");
            }

            if (!ModelState.IsValid)
            {
                await PopulateFestivalSelectListAsync(package.FestivalId);
                return View(package);
            }

            try
            {
                _context.Update(package);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                // Just checking if it wasn't deleted by someone else in the meantime.
                if (!PackageExists(package.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(Index), new { festivalId = package.FestivalId });
        }

        // GET: Packages/Delete/5
        // Asks "Are you sure?" before deleting a package.
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var package = await _context.Packages
                .Include(p => p.Festival)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (package == null)
            {
                return NotFound();
            }

            return View(package);
        }

        // POST: Packages/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        // This is where the package actually gets deleted. Bye bye!
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var package = await _context.Packages
                .Include(p => p.PackageItems)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (package == null)
            {
                return RedirectToAction(nameof(Index));
            }
            try
            {
                // We clear out the items in the package first.
                if (package.PackageItems.Any())
                {
                    _context.PackageItems.RemoveRange(package.PackageItems);
                }

                _context.Packages.Remove(package);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index), new { festivalId = package.FestivalId });
            }
            catch (DbUpdateException)
            {
                TempData["Error"] = "Kan pakket niet verwijderen: er zijn nog gekoppelde items.";
                return RedirectToAction(nameof(Index), new { festivalId = package.FestivalId });
            }
        }

        // Helper to fill that festival dropdown list we use everywhere.
        private async Task PopulateFestivalSelectListAsync(int? selectedId = null)
        {
            var festivals = await _context.Festivals
                .AsNoTracking()
                .OrderBy(f => f.Name)
                .ToListAsync();

            ViewData["FestivalId"] = new SelectList(festivals, "Id", "Name", selectedId);
        }

        // quick check to see if a package exists.
        private bool PackageExists(int id)
        {
            return _context.Packages.Any(e => e.Id == id);
        }

    // GET: /Packages/Ticket/5
    [HttpGet]
    // This helper grabs the ticket details for you. It checks the database for the package 
    // and includes all the extra goodies (items & festival info).
    // If the package is missing, it just shrugs and returns a Not Found.
    public async Task<IActionResult> Ticket(int id)
    {
        var pkg = await _context.Packages
            .Include(p => p.Festival)
            .Include(p => p.PackageItems)
                .ThenInclude(pi => pi.Item)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (pkg == null) return NotFound("Pakket niet gevonden.");

        // Grabbing all the available items, grouped by type so we can display them nicely.
        var catalogs = await _context.Items
            .AsNoTracking()
            .GroupBy(i => i.ItemType)
            .ToDictionaryAsync(g => g.Key, g => g.OrderBy(i => i.Name).ToList());

        var vm = new TicketViewModel
        {
            PackageId = pkg.Id,
            PackageName = pkg.Name,
            FestivalId = pkg.FestivalId,
            FestivalName = pkg.Festival!.Name,
            FestivalPlace = pkg.Festival!.Place,
            FestivalLogo = pkg.Festival!.Logo,
            FestivalDescription = pkg.Festival!.Description,
            StartDate = pkg.Festival!.StartDate,
            EndDate = pkg.Festival!.EndDate,
            BasicPrice = pkg.Festival!.BasicPrice,
        };

        // Loop through every ItemType (Tent, Ticket, etc.) to build the panels.
        foreach (ItemType t in Enum.GetValues<ItemType>())
        {
            var existing = pkg.PackageItems.FirstOrDefault(pi => pi.Item.ItemType == t);

            // Create a dropdown list for this specific item type.
            var options = catalogs.TryGetValue(t, out var list)
                ? list.Select(i => new SelectListItem
                {
                    Text = $"{i.Name} ({i.Price:C})",
                    Value = i.Id.ToString(),
                    Selected = existing?.ItemId == i.Id
                }).ToList()
                : new List<SelectListItem>();

            vm.Panels.Add(new TicketPanelViewModel
            {
                Type = t,
                SelectedItemId = existing?.ItemId,
                SelectedItemName = existing?.Item.Name,
                UnitPrice = existing?.Item.Price,
                Quantity = existing?.Quantity ?? 0,
                Options = options,
                IconPath = $"/img/icons/{t}.svg"
            });
        }

        // Calculate the total price: basic festival price + cost of selected items.
        vm.PackageTotal = vm.BasicPrice + pkg.PackageItems.Sum(pi => pi.Item.Price * pi.Quantity);

        return View(vm);
    }

    // POST: /Packages/Ticket
    [HttpPost]
    [ValidateAntiForgeryToken]
    // Consumes the logic for when you update the ticket (add/remove items).
    // This method handles all the changes you make to your package's items (adding, removing, or changing quantities).
    public async Task<IActionResult> Ticket(TicketPostModel form)
    {
        var pkg = await _context.Packages
            .Include(p => p.PackageItems)
                .ThenInclude(pi => pi.Item)
            .Include(p => p.Festival)
            .FirstOrDefaultAsync(p => p.Id == form.PackageId);
        if (pkg == null) return NotFound("Pakket niet gevonden.");

        var existing = pkg.PackageItems.FirstOrDefault(pi => pi.Item.ItemType == form.Type);

        // If command is 'deselect' or no valid item selected, we clear the selection.
        if (string.Equals(form.Command, "deselect", StringComparison.OrdinalIgnoreCase) || form.ItemId is null || form.Quantity <= 0)
        {
            if (existing != null)
                _context.PackageItems.Remove(existing);
        }
        else
        {
            // Otherwise, we fetch the new item to make sure it exists and matches the type.
            var item = await _context.Items.FirstOrDefaultAsync(i => i.Id == form.ItemId);
            if (item == null) return BadRequest("Dit item bestaat niet meer; je selectie is gewist.");
            if (item.ItemType != form.Type) return BadRequest("Ongeldige selectie voor dit type.");

            if (existing != null)
                _context.PackageItems.Remove(existing);

            // Add the new selection to the package.
            _context.PackageItems.Add(new PackageItem
            {
                PackageId = pkg.Id,
                ItemId = item.Id,
                Quantity = form.Quantity
            });
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Ticket), new { id = pkg.Id });
    }
}
}
