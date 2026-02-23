using FestivalTickets.Domain;
using FestivalTickets.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FestivalTickets.Infrastructure.Repositories;

public sealed class BookingRepository : IBookingRepository
{
    private readonly ApplicationDbContext _db;
    public BookingRepository(ApplicationDbContext db) => _db = db;

    public async Task<Booking?> GetByIdAsync(int id) =>
        await _db.Bookings
            .Include(b => b.Package).ThenInclude(p => p!.Festival)
            .Include(b => b.Package).ThenInclude(p => p!.PackageItems).ThenInclude(pi => pi.Item)
            .Include(b => b.Customer)
            .Include(b => b.BookingItems).ThenInclude(bi => bi.Item)
            .FirstOrDefaultAsync(b => b.Id == id);

    public async Task<IEnumerable<Booking>> GetAllAsync() =>
        await _db.Bookings
            .Include(b => b.Package).ThenInclude(p => p!.Festival)
            .Include(b => b.Customer)
            .OrderByDescending(b => b.BookingDate)
            .ToListAsync();

    public async Task<IEnumerable<Booking>> GetByCustomerIdAsync(int customerId) =>
        await _db.Bookings
            .Include(b => b.Package).ThenInclude(p => p!.Festival)
            .Include(b => b.BookingItems).ThenInclude(bi => bi.Item)
            .Where(b => b.CustId == customerId)
            .OrderByDescending(b => b.BookingDate)
            .ToListAsync();

    public async Task AddAsync(Booking booking)
    {
        await _db.Bookings.AddAsync(booking);
    }

    public async Task DeleteAsync(int id)
    {
        var booking = await _db.Bookings
            .Include(b => b.BookingItems)
            .FirstOrDefaultAsync(b => b.Id == id);
        if (booking == null) return;

        _db.BookingItems.RemoveRange(booking.BookingItems);
        _db.Bookings.Remove(booking);
    }

    public async Task SaveChangesAsync() => await _db.SaveChangesAsync();
}
