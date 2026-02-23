using System.Collections.Generic;
using System.Threading.Tasks;

namespace FestivalTickets.Domain.Interfaces;

public interface IItemRepository
{
    Task<IEnumerable<Item>> GetAllAsync();
}
