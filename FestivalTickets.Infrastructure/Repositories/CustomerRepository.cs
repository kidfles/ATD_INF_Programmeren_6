using FestivalTickets.Domain;
using FestivalTickets.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FestivalTickets.Infrastructure.Repositories;

public sealed class CustomerRepository : ICustomerRepository
{
    private readonly ApplicationDbContext _db;
    public CustomerRepository(ApplicationDbContext db) => _db = db;

    public async Task<Customer?> GetByUserIdAsync(string userId) =>
        await _db.Customers.FirstOrDefaultAsync(c => c.UserId == userId);

    public async Task<Customer?> GetByIdAsync(int id) =>
        await _db.Customers.FindAsync(id);

    public async Task<IEnumerable<Customer>> GetAllAsync() =>
        await _db.Customers.ToListAsync();

    public async Task AddAsync(Customer customer) =>
        await _db.Customers.AddAsync(customer);

    public async Task SaveChangesAsync() => await _db.SaveChangesAsync();
}
