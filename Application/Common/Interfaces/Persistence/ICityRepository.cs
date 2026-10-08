using Application.Common.Models;
using Domain.Entities;

namespace Application.Common.Interfaces.Persistence;

public interface ICityRepository : IRepository<City, int>
{
    Task<PaginatedList<City>> SearchAsync(string? nameFilter, int pageNumber, int pageSize,
        CancellationToken cancellationToken = default);

    Task<bool> NameExistsAsync(string name, int? excludeId = null, CancellationToken cancellationToken = default);
    Task<bool> HasHotelsAsync(int cityId, CancellationToken cancellationToken = default);
}