using FestivalTickets.Domain;
using FestivalTickets.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FestivalTickets.Infrastructure.Repositories;

public sealed class PackageRepository : IPackageRepository
{
    private readonly ApplicationDbContext _db;
    public PackageRepository(ApplicationDbContext db) => _db = db;

    public async Task<IEnumerable<Package>> GetByFestivalIdAsync(int festivalId) =>
        await _db.Packages
            .Where(p => p.FestivalId == festivalId)
            .Include(p => p.PackageItems).ThenInclude(pi => pi.Item)
            .ToListAsync();

    public async Task<Package?> GetByIdWithItemsAsync(int id) =>
        await _db.Packages
            .Include(p => p.PackageItems).ThenInclude(pi => pi.Item)
            .Include(p => p.Festival)
            .FirstOrDefaultAsync(p => p.Id == id);
}
