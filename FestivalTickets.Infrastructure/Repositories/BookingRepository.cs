using FestivalTickets.Domain;
using FestivalTickets.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FestivalTickets.Infrastructure.Repositories;

public sealed class BookingRepository : IBookingRepository
{
    private readonly ApplicationDbContext _context;

    public BookingRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Booking?> GetByIdAsync(int id)
    {
        return await QueryWithDetails()
            .FirstOrDefaultAsync(b => b.Id == id);
    }

    public async Task<IEnumerable<Booking>> GetAllAsync()
    {
        return await QueryWithDetails()
            .OrderByDescending(b => b.BookingDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Booking>> GetByCustomerIdAsync(int customerId)
    {
        return await QueryWithDetails()
            .Where(b => b.CustId == customerId)
            .OrderByDescending(b => b.BookingDate)
            .ToListAsync();
    }

    public async Task AddAsync(Booking booking)
    {
        await _context.Bookings.AddAsync(booking);
    }

    public async Task DeleteAsync(int id)
    {
        var booking = await _context.Bookings
            .Include(b => b.BookingItems)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (booking == null)
        {
            return;
        }

        if (booking.BookingItems.Count > 0)
        {
            _context.BookingItems.RemoveRange(booking.BookingItems);
        }

        _context.Bookings.Remove(booking);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    private IQueryable<Booking> QueryWithDetails()
    {
        return _context.Bookings
            .AsNoTracking()
            .Include(b => b.Customer)
            .Include(b => b.Package)
                .ThenInclude(p => p!.Festival)
            .Include(b => b.BookingItems)
                .ThenInclude(bi => bi.Item);
    }
}
