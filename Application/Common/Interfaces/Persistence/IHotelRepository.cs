using Application.Common.Models;
using Domain.Entities;

namespace Application.Common.Interfaces.Persistence;

public interface IHotelRepository : IRepository<Hotel, int>
{
    /// <summary>
    /// Must eager-load: City, Owner, HotelAmenities+Amenity, Reviews+User, Images, Rooms+RoomImages+Discounts+Availabilities.
    /// </summary>
    Task<Hotel?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tracked load with HotelAmenities eager-loaded, so clearing/adding amenity links is
    /// change-tracked and persisted (unlike the AsNoTracking GetByIdWithDetailsAsync).
    /// </summary>
    Task<Hotel?> GetByIdWithAmenitiesTrackedAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tracked load with Images eager-loaded, so adding/removing a gallery image is change-tracked
    /// and persisted (removing from the AsNoTracking GetByIdWithDetailsAsync graph never issued a DELETE).
    /// </summary>
    Task<Hotel?> GetByIdWithImagesTrackedAsync(int id, CancellationToken cancellationToken = default);

    Task<PaginatedList<Hotel>> SearchAsync(HotelSearchFilter filter, int pageNumber, int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Only Approved hotels with at least one currently-active Discount on an active Room.
    /// Must eager-load Rooms+Discounts+Images so the handler can compute original/discounted price.
    /// </summary>
    Task<IReadOnlyList<Hotel>> GetFeaturedDealsAsync(int count, CancellationToken cancellationToken = default);

    Task<bool> IsOwnedByAsync(int hotelId, Guid ownerId, CancellationToken cancellationToken = default);

    /// <summary>True if any room row (active or soft-deleted) still belongs to the hotel.</summary>
    Task<bool> HasRoomsAsync(int hotelId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Number of room rows (active or soft-deleted) belonging to the hotel — the same count the
    /// list endpoints' <c>RoomsCount</c> reports via the eager-loaded Rooms collection.
    /// </summary>
    Task<int> CountRoomsAsync(int hotelId, CancellationToken cancellationToken = default);

    Task<PaginatedList<Hotel>> GetByOwnerIdAsync(Guid ownerId, int pageNumber, int pageSize,
        CancellationToken cancellationToken = default);

    Task<PaginatedList<Hotel>> GetPendingApprovalAsync(int pageNumber, int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Every hotel regardless of approval status (admin grid), newest first.
    /// </summary>
    Task<PaginatedList<Hotel>> GetAllForAdminAsync(HotelAdminFilter filter, int pageNumber, int pageSize,
        CancellationToken cancellationToken = default);
}