using Application.Common.Models;
using Domain.Entities;

namespace Application.Common.Interfaces.Persistence;

public interface ICityRepository : IRepository<City, int>
{
    Task<PaginatedList<City>> SearchAsync(string? nameFilter, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<bool> NameExistsAsync(string name, CancellationToken cancellationToken = default);
}