using Application.Common.Models;
using Application.Common.Specifications;
using Domain.Entities;
using Domain.Enums;

namespace Application.Features.Hotels.Specifications;

public class HotelSearchSpecification : BaseSpecification<Hotel>
{
    public HotelSearchSpecification(HotelSearchFilter filter, int pageNumber, int pageSize)
    {
        AddCriteria(h => h.ApprovalStatus == HotelApprovalStatus.Approved);

        if (!string.IsNullOrWhiteSpace(filter.Keyword))
            AddCriteria(h => h.Name.Contains(filter.Keyword) || h.City.Name.Contains(filter.Keyword));

        if (filter.CityId.HasValue)
            AddCriteria(h => h.CityId == filter.CityId.Value);

        if (filter.MinStarRating.HasValue)
            AddCriteria(h => h.StarRating >= filter.MinStarRating.Value);

        if (filter.RoomType.HasValue)
            AddCriteria(h => h.Rooms.Any(r => r.IsActive && r.RoomType == filter.RoomType.Value));

        if (filter.AmenityIds is { Count: > 0 })
            AddCriteria(h => filter.AmenityIds.All(id => h.HotelAmenities.Any(ha => ha.AmenityId == id)));

        // Price range and date-based availability are deliberately NOT here — they depend on
        // Room.GetActivePrice() / Room.IsAvailableFor(range), domain methods that can't translate
        // into SQL. The repository implementation applies these as a post-materialization filter
        // once results are loaded (or via a raw availability-table subquery for dates specifically) —
        // handled when we build HotelRepository.SearchAsync, not here in the spec.

        AddInclude(h => h.City);
        AddInclude(h => h.Owner);
        AddInclude(h => h.Images);
        AddInclude(h => h.Rooms);

        ApplyOrderBy(h => h.StarRating);
        ApplyPaging((pageNumber - 1) * pageSize, pageSize);
    }
}