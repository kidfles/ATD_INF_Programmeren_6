using FestivalTickets.Domain;
using FestivalTickets.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FestivalTickets.Infrastructure.Repositories;

public sealed class FestivalRepository : IFestivalRepository
{
    private readonly ApplicationDbContext _db;
    public FestivalRepository(ApplicationDbContext db) => _db = db;

    public async Task<IEnumerable<Festival>> GetAllAsync() =>
        await _db.Festivals.ToListAsync();

    public async Task<IEnumerable<Festival>> GetUpcomingAsync(DateOnly from, DateOnly to) =>
        await _db.Festivals
            .Where(f => f.StartDate >= from && f.StartDate <= to)
            .OrderBy(f => f.StartDate)
            .ToListAsync();

    public async Task<Festival?> GetByIdWithPackagesAsync(int id) =>
        await _db.Festivals
            .Include(f => f.Packages)
                .ThenInclude(p => p.PackageItems)
                    .ThenInclude(pi => pi.Item)
            .FirstOrDefaultAsync(f => f.Id == id);
}
