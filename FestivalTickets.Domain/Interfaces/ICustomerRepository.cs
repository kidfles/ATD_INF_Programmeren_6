using System.Collections.Generic;
using System.Threading.Tasks;

namespace FestivalTickets.Domain.Interfaces;

public interface ICustomerRepository
{
    Task<Customer?> GetByUserIdAsync(string userId);
    Task<Customer?> GetByIdAsync(int id);
    Task<IEnumerable<Customer>> GetAllAsync();
    Task AddAsync(Customer customer);
    Task SaveChangesAsync();
}
