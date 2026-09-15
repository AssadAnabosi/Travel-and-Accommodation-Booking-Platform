using Application.Common.Models;
using Domain.Entities;

namespace Application.Common.Interfaces.Persistence;

public interface IHotelRepository : IRepository<Hotel, int>
{
    /// <summary>
    /// Includes City, HotelAmenities, Reviews for the Hotel Detail page.
    /// </summary>
    Task<Hotel?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default);

    Task<PaginatedList<Hotel>> SearchAsync(HotelSearchFilter filter, int pageNumber, int pageSize,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Hotel>> GetFeaturedDealsAsync(int count, CancellationToken cancellationToken = default);
    Task<bool> IsOwnedByAsync(int hotelId, Guid ownerId, CancellationToken cancellationToken = default);

    Task<PaginatedList<Hotel>> GetByOwnerIdAsync(Guid ownerId, int pageNumber, int pageSize,
        CancellationToken cancellationToken = default);

    Task<PaginatedList<Hotel>> GetPendingApprovalAsync(int pageNumber, int pageSize,
        CancellationToken cancellationToken = default);
}