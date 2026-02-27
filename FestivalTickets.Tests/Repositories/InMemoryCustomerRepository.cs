using FestivalTickets.Domain;
using FestivalTickets.Domain.Interfaces;

namespace FestivalTickets.Tests.Repositories;

public sealed class InMemoryCustomerRepository : ICustomerRepository
{
    private readonly List<Customer> _store = new();

    public Task<Customer?> GetByUserIdAsync(string userId) =>
        Task.FromResult(_store.FirstOrDefault(c => c.UserId == userId));

    public Task<Customer?> GetByIdAsync(int id) =>
        Task.FromResult(_store.FirstOrDefault(c => c.Id == id));

    public Task<IEnumerable<Customer>> GetAllAsync() =>
        Task.FromResult<IEnumerable<Customer>>(_store.ToList());

    public Task AddAsync(Customer customer)
    {
        customer.Id = _store.Count + 1;
        _store.Add(customer);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync() => Task.CompletedTask;
}
