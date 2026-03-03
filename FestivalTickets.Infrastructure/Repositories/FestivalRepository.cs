using FestivalTickets.Domain;
using FestivalTickets.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FestivalTickets.Infrastructure.Repositories;

public sealed class FestivalRepository : IFestivalRepository
{
    private readonly ApplicationDbContext _context;

    public FestivalRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Festival>> GetAllAsync()
    {
        return await _context.Festivals
            .AsNoTracking()
            .OrderBy(f => f.StartDate)
            .ThenBy(f => f.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<Festival>> GetUpcomingAsync(DateOnly from, DateOnly to)
    {
        return await _context.Festivals
            .AsNoTracking()
            .Where(f => f.StartDate >= from && f.StartDate <= to)
            .OrderBy(f => f.StartDate)
            .ThenBy(f => f.Name)
            .ToListAsync();
    }

    public async Task<Festival?> GetByIdWithPackagesAsync(int id)
    {
        return await _context.Festivals
            .AsNoTracking()
            .Include(f => f.Packages)
                .ThenInclude(p => p.PackageItems)
                    .ThenInclude(pi => pi.Item)
            .FirstOrDefaultAsync(f => f.Id == id);
    }
}
