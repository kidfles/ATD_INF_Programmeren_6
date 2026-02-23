using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FestivalTickets.Domain;
using FestivalTickets.Infrastructure;
using FestivalTickets.Web.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FestivalTickets.Web.Controllers
{
    // Handles everything related to Festivals. Creating, editing, you name it.
    public class FestivalsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public FestivalsController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // GET: Festivals
        // Shows a list of all the festivals we have.
        public async Task<IActionResult> Index()
        {
            return View(await _context.Festivals.ToListAsync());
        }

        // GET: Festivals/Details/5
        // Shows the details for one specific festival.
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var festival = await _context.Festivals
                .FirstOrDefaultAsync(m => m.Id == id);
            if (festival == null)
            {
                return NotFound();
            }

            return View(festival);
        }

        // GET: Festivals/Create
        // Sets up the form to add a new festival.
        public IActionResult Create()
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var vm = new FestivalFormViewModel
            {
                StartDate = today,
                EndDate = today.AddDays(1)
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        // This accepts the form data and saves the new festival.
        // Also handles the logo upload.
        public async Task<IActionResult> Create(FestivalFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Try to save the logo if one was uploaded.
            var (logoSaved, logoPath) = await TrySaveLogoAsync(model.LogoFile);
            if (!logoSaved)
            {
                // If the logo failed (e.g. wrong format), we complain and show the form again.
                model.ExistingLogoPath = null;
                return View(model);
            }

            var festival = new Festival
            {
                Name = model.Name,
                Place = model.Place,
                Description = model.Description,
                BasicPrice = model.BasicPrice,
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                Logo = logoPath
            };

            _context.Add(festival);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Festivals/Edit/5
        // Opens the form to edit an existing festival.
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var festival = await _context.Festivals.AsNoTracking().FirstOrDefaultAsync(f => f.Id == id);
            if (festival == null)
            {
                return NotFound();
            }

            var vm = new FestivalFormViewModel
            {
                Id = festival.Id,
                Name = festival.Name,
                Place = festival.Place,
                Description = festival.Description,
                BasicPrice = festival.BasicPrice,
                StartDate = festival.StartDate,
                EndDate = festival.EndDate,
                ExistingLogoPath = festival.Logo
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        // Saves the changes to the festival.
        public async Task<IActionResult> Edit(int id, FestivalFormViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var festival = await _context.Festivals.FindAsync(id);
            if (festival == null)
            {
                return NotFound();
            }

            // Check if there's a new logo to save.
            var (logoSaved, logoPath) = await TrySaveLogoAsync(model.LogoFile);
            if (!logoSaved)
            {
                model.ExistingLogoPath = festival.Logo;
                return View(model);
            }

            festival.Name = model.Name;
            festival.Place = model.Place;
            festival.Description = model.Description;
            festival.BasicPrice = model.BasicPrice;
            festival.StartDate = model.StartDate;
            festival.EndDate = model.EndDate;
            // Only overwrite the logo if a new one was actually uploaded.
            festival.Logo = logoPath ?? festival.Logo;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!FestivalExists(festival.Id))
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

        // GET: Festivals/Delete/5
        // Asks if you're sure about deleting this festival.
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var festival = await _context.Festivals
                .FirstOrDefaultAsync(m => m.Id == id);
            if (festival == null)
            {
                return NotFound();
            }

            return View(festival);
        }

        // POST: Festivals/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        // This actually wipes the festival from the database.
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var festival = await _context.Festivals.FindAsync(id);
            if (festival == null)
            {
                return RedirectToAction(nameof(Index));
            }
            try
            {
                _context.Festivals.Remove(festival);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                // Can't delete if there are packages linked to it.
                TempData["Error"] = "Kan festival niet verwijderen: er zijn nog gekoppelde pakketten of items.";
                return RedirectToAction(nameof(Index));
            }
        }

        // Helper to save the uploaded logo file to the wwwroot/img/logos folder.
        // Returns true if success or empty, false if invalid format.
        private async Task<(bool Saved, string? Path)> TrySaveLogoAsync(IFormFile? file)
        {
            if (file == null || file.Length == 0)
            {
                return (true, null);
            }

            // We only like PNGs here.
            var isPng = string.Equals(file.ContentType, "image/png", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(Path.GetExtension(file.FileName), ".png", StringComparison.OrdinalIgnoreCase);

            if (!isPng)
            {
                ModelState.AddModelError(nameof(FestivalFormViewModel.LogoFile), "Upload een PNG-bestand (.png).");
                return (false, null);
            }

            var logosFolder = Path.Combine(_environment.WebRootPath, "img", "logos");
            Directory.CreateDirectory(logosFolder);

            // Give it a unique name so we don't overwrite other stuff.
            var fileName = $"{Guid.NewGuid():N}.png";
            var fullPath = Path.Combine(logosFolder, fileName);

            await using var stream = System.IO.File.Create(fullPath);
            await file.CopyToAsync(stream);

            return (true, $"/img/logos/{fileName}");
        }

        // Check if a festival exists by ID.
        private bool FestivalExists(int id)
        {
            return _context.Festivals.Any(e => e.Id == id);
        }
    }
}
