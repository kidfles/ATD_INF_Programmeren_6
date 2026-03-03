using FestivalTickets.Domain;
using FestivalTickets.Domain.Interfaces;

namespace FestivalTickets.Tests.Repositories;

/// <summary>
/// In-memory implementation of IBookingRepository for tests.
/// No database needed.
/// </summary>
public sealed class InMemoryBookingRepository : IBookingRepository
{
    private readonly List<Booking> _store = new();

    public Task<Booking?> GetByIdAsync(int id) =>
        Task.FromResult(_store.FirstOrDefault(b => b.Id == id));

    public Task<IEnumerable<Booking>> GetAllAsync() =>
        Task.FromResult<IEnumerable<Booking>>(_store.ToList());

    public Task<IEnumerable<Booking>> GetByCustomerIdAsync(int customerId) =>
        Task.FromResult<IEnumerable<Booking>>(_store.Where(b => b.CustId == customerId).ToList());

    public Task AddAsync(Booking booking)
    {
        booking.Id = _store.Count + 1;
        _store.Add(booking);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(int id)
    {
        var booking = _store.FirstOrDefault(x => x.Id == id);
        if (booking != null)
        {
            _store.Remove(booking);
        }

        return Task.CompletedTask;
    }

    public Task SaveChangesAsync() => Task.CompletedTask;
}
