using FestivalTickets.Domain;
using FestivalTickets.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FestivalTickets.Infrastructure.Repositories;

public sealed class ItemRepository : IItemRepository
{
    private readonly ApplicationDbContext _db;
    public ItemRepository(ApplicationDbContext db) => _db = db;

    public async Task<IEnumerable<Item>> GetAllAsync() =>
        await _db.Items.OrderBy(i => i.ItemType).ThenBy(i => i.Name).ToListAsync();
}
