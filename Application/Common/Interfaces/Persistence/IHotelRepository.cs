using Application.Common.Models;
using Domain.Entities;

namespace Application.Common.Interfaces.Persistence;

public interface IHotelRepository : IRepository<Hotel, int>
{
    /// <summary>
    /// Must eager-load: City, Owner, HotelAmenities+Amenity, Reviews+User, Images, Rooms+RoomImages+Discounts+Availabilities.
    /// </summary>
    Task<Hotel?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default);

    Task<PaginatedList<Hotel>> SearchAsync(HotelSearchFilter filter, int pageNumber, int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Only Approved hotels with at least one currently-active Discount on an active Room.
    /// Must eager-load Rooms+Discounts+Images so the handler can compute original/discounted price.
    /// </summary>
    Task<IReadOnlyList<Hotel>> GetFeaturedDealsAsync(int count, CancellationToken cancellationToken = default);

    Task<bool> IsOwnedByAsync(int hotelId, Guid ownerId, CancellationToken cancellationToken = default);

    Task<PaginatedList<Hotel>> GetByOwnerIdAsync(Guid ownerId, int pageNumber, int pageSize,
        CancellationToken cancellationToken = default);

    Task<PaginatedList<Hotel>> GetPendingApprovalAsync(int pageNumber, int pageSize,
        CancellationToken cancellationToken = default);
}