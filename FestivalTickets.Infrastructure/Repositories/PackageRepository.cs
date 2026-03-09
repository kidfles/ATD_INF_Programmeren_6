using FestivalTickets.Domain;
using FestivalTickets.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FestivalTickets.Infrastructure.Repositories;

public sealed class PackageRepository : IPackageRepository
{
    private readonly ApplicationDbContext _context;

    public PackageRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Package>> GetByFestivalIdAsync(int festivalId)
    {
        return await _context.Packages
            .AsNoTracking()
            .Where(p => p.FestivalId == festivalId)
            .Include(p => p.PackageItems)
                .ThenInclude(pi => pi.Item)
            .OrderBy(p => p.Name)
            .ToListAsync();
    }

    public async Task<Package?> GetByIdWithItemsAsync(int id)
    {
        return await _context.Packages
            .AsNoTracking()
            .Include(p => p.Festival)
            .Include(p => p.PackageItems)
                .ThenInclude(pi => pi.Item)
            .FirstOrDefaultAsync(p => p.Id == id);
    }
}
