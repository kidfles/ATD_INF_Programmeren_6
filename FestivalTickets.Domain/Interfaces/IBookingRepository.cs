using System.Collections.Generic;
using System.Threading.Tasks;

namespace FestivalTickets.Domain.Interfaces;

public interface IBookingRepository
{
    Task<Booking?> GetByIdAsync(int id);
    Task<IEnumerable<Booking>> GetAllAsync();
    Task<IEnumerable<Booking>> GetByCustomerIdAsync(int customerId);
    Task AddAsync(Booking booking);
    Task DeleteAsync(int id);
    Task SaveChangesAsync();
}
