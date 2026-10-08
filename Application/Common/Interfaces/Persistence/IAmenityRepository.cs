using Domain.Entities;

namespace Application.Common.Interfaces.Persistence;

public interface IAmenityRepository : IRepository<Amenity, int>
{
    Task<IReadOnlyList<Amenity>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<bool> NameExistsAsync(string name, int? excludeId = null, CancellationToken cancellationToken = default);
    Task<bool> IsInUseAsync(int amenityId, CancellationToken cancellationToken = default);
    Task<bool> AllExistAsync(IEnumerable<int> amenityIds, CancellationToken cancellationToken = default);
}