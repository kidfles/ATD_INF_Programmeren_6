using FestivalTickets.Domain;
using FestivalTickets.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FestivalTickets.Infrastructure.Repositories;

/// <summary>
/// EF Core implementatie van IBookingRepository.
/// Laadt altijd gerelateerde entiteiten mee via QueryWithDetails().
/// </summary>
public sealed class BookingRepository : IBookingRepository
{
    private readonly ApplicationDbContext _context;

    public BookingRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>Geeft één boeking inclusief klant, pakket, festival en boekingsitems.</summary>
    public async Task<Booking?> GetByIdAsync(int id)
    {
        return await QueryWithDetails()
            .FirstOrDefaultAsync(b => b.Id == id);
    }

    /// <summary>Geeft alle boekingen, gesorteerd op boekingsdatum (nieuwste eerst).</summary>
    public async Task<IEnumerable<Booking>> GetAllAsync()
    {
        return await QueryWithDetails()
            .OrderByDescending(b => b.BookingDate)
            .ToListAsync();
    }

    /// <summary>Geeft alle boekingen van één klant, gesorteerd op boekingsdatum (nieuwste eerst).</summary>
    public async Task<IEnumerable<Booking>> GetByCustomerIdAsync(int customerId)
    {
        return await QueryWithDetails()
            .Where(b => b.CustId == customerId)
            .OrderByDescending(b => b.BookingDate)
            .ToListAsync();
    }

    /// <summary>Voegt een nieuwe boeking toe aan de context (nog niet opgeslagen).</summary>
    public async Task AddAsync(Booking booking)
    {
        await _context.Bookings.AddAsync(booking);
    }

    /// <summary>
    /// Verwijdert een boeking en alle bijbehorende boekingsitems.
    /// Doet niets als de boeking niet bestaat.
    /// </summary>
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

    /// <summary>Slaat alle openstaande wijzigingen op in de database.</summary>
    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    // Herbruikbare query met alle benodigde includes voor volledige boekingsdata.
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
