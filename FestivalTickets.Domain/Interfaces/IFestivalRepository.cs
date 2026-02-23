using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FestivalTickets.Domain.Interfaces;

public interface IFestivalRepository
{
    Task<IEnumerable<Festival>> GetAllAsync();
    Task<IEnumerable<Festival>> GetUpcomingAsync(DateOnly from, DateOnly to);
    Task<Festival?> GetByIdWithPackagesAsync(int id);
}
