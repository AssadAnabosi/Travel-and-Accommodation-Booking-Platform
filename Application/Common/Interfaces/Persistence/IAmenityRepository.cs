using Domain.Entities;

namespace Application.Common.Interfaces.Persistence;

public interface IAmenityRepository : IRepository<Amenity, int>
{
    Task<IReadOnlyList<Amenity>> GetAllAsync(CancellationToken cancellationToken = default);
}