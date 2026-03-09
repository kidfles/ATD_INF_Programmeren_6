using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using FestivalTickets.Domain;
using FestivalTickets.Infrastructure;
using Microsoft.AspNetCore.Authorization;

namespace FestivalTickets.Web.Controllers
{
    // This controller manages all the items (like tents, tickets, parking spots, etc.).
    [Authorize(Roles = "Administrator")]
    public class ItemsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ItemsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Items
        // Shows a list of all items. You can search, filter by type, and sort them.
        public async Task<IActionResult> Index(ItemType? type, string? search, string? sort)
        {
            ViewData["CurrentType"] = type;
            ViewData["CurrentSearch"] = search;
            // Toggling the sort order (ascending vs descending) for the column headers.
            ViewData["NameSort"] = sort == "name_desc" ? "name_asc" : "name_desc";
            ViewData["TypeSort"] = sort == "type_desc" ? "type_asc" : "type_desc";
            ViewData["PriceSort"] = sort == "price_desc" ? "price_asc" : "price_desc";

            IQueryable<Item> query = _context.Items.AsNoTracking();

            // Filter by item type if one is selected.
            if (type.HasValue)
            {
                query = query.Where(i => i.ItemType == type.Value);
            }

            // Filter by search text if something was typed in.
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(i => i.Name.Contains(search));
            }

            // Handle the sorting logic.
            query = sort switch
            {
                "name_asc" => query.OrderBy(i => i.Name),
                "name_desc" => query.OrderByDescending(i => i.Name),
                "type_asc" => query.OrderBy(i => i.ItemType).ThenBy(i => i.Name),
                "type_desc" => query.OrderByDescending(i => i.ItemType).ThenBy(i => i.Name),
                "price_asc" => query.OrderBy(i => i.Price).ThenBy(i => i.Name),
                "price_desc" => query.OrderByDescending(i => i.Price).ThenBy(i => i.Name),
                _ => query.OrderBy(i => i.ItemType).ThenBy(i => i.Name)
            };

            var items = await query.ToListAsync();
            return View(items);
        }

        // GET: Items/Details/5
        // Shows the details of a specific item.
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var item = await _context.Items
                .FirstOrDefaultAsync(m => m.Id == id);
            if (item == null)
            {
                return NotFound();
            }

            return View(item);
        }

        // GET: Items/Create
        // Opens the form to add a new item.
        public IActionResult Create()
        {
            return View();
        }

        // POST: Items/Create

        [HttpPost]
        [ValidateAntiForgeryToken]
        // Saves the new item to the database.
        public async Task<IActionResult> Create([Bind("Id,Name,ItemType,Price")] Item item)
        {
            if (ModelState.IsValid)
            {
                _context.Add(item);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(item);
        }

        // GET: Items/Edit/5
        // Opens the form to edit an existing item.
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var item = await _context.Items.FindAsync(id);
            if (item == null)
            {
                return NotFound();
            }
            return View(item);
        }

        // POST: Items/Edit/5

        [HttpPost]
        [ValidateAntiForgeryToken]
        // Saves any changes you made to the item.
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,ItemType,Price")] Item item)
        {
            if (id != item.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(item);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ItemExists(item.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(item);
        }

        // GET: Items/Delete/5
        // Asks nicely if you really want to delete this item.
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var item = await _context.Items
                .FirstOrDefaultAsync(m => m.Id == id);
            if (item == null)
            {
                return NotFound();
            }

            return View(item);
        }

        // POST: Items/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        // Actually deletes the item. Checks if it's being used first.
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var item = await _context.Items.FindAsync(id);
            if (item == null)
            {
                return RedirectToAction(nameof(Index));
            }
            try
            {
                _context.Items.Remove(item);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                // Can't delete if it's used in a package somewhere.
                TempData["Error"] = "Kan item niet verwijderen: het item is gebruikt in pakketten.";
                return RedirectToAction(nameof(Index));
            }
        }

        // Checks if an item exists by ID.
        private bool ItemExists(int id)
        {
            return _context.Items.Any(e => e.Id == id);
        }
    }
}
