using System.Collections.Generic;
using System.Threading.Tasks;

namespace FestivalTickets.Domain.Interfaces;

public interface IPackageRepository
{
    Task<IEnumerable<Package>> GetByFestivalIdAsync(int festivalId);
    Task<Package?> GetByIdWithItemsAsync(int id);
}
