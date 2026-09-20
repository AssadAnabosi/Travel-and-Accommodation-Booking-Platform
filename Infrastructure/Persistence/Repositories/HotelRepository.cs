using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.Common.Models;
using Application.Features.Hotels.Specifications;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class HotelRepository(AppDbContext context, IDateTimeProvider dateTimeProvider) : IHotelRepository
{
    public async Task<Hotel?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        await context.Hotels.FirstOrDefaultAsync(h => h.Id == id, cancellationToken);

    public async Task AddAsync(Hotel entity, CancellationToken cancellationToken = default) =>
        await context.Hotels.AddAsync(entity, cancellationToken);

    public void Update(Hotel entity) => context.Hotels.Update(entity);

    public void Remove(Hotel entity) => context.Hotels.Remove(entity);
    
    public async Task<Hotel?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default) =>
        await context.Hotels
            .AsNoTracking()
            .AsSplitQuery()
            .Include(h => h.City)
            .Include(h => h.Owner)
            .Include(h => h.HotelAmenities).ThenInclude(ha => ha.Amenity)
            .Include(h => h.Reviews).ThenInclude(r => r.User)
            .Include(h => h.Images)
            .Include(h => h.Rooms).ThenInclude(r => r.Images)
            .Include(h => h.Rooms).ThenInclude(r => r.Discounts)
            .Include(h => h.Rooms).ThenInclude(r => r.Availabilities)
            .FirstOrDefaultAsync(h => h.Id == id, cancellationToken);

    public async Task<PaginatedList<Hotel>> SearchAsync(HotelSearchFilter filter, int pageNumber, int pageSize,
        CancellationToken cancellationToken = default)
    {
        // The specification carries the purely SQL-translatable filters (approval, keyword, city, star,
        // room-type, amenities) plus the includes/ordering the handler needs. Paging is deferred so the
        // domain-dependent filters below still count against the full result set.
        var specification = new HotelSearchSpecification(filter, pageNumber, pageSize);
        var query = SpecificationEvaluator<Hotel>.GetQuery(
            context.Hotels.AsNoTracking(), specification, evaluatePaging: false);

        // Price is evaluated on the check-in date (or today when no dates are given); when several
        // discounts overlap that date we take the best (lowest) resulting price.
        var priceDate = filter.CheckIn ?? dateTimeProvider.Today;
        var checkIn = filter.CheckIn ?? DateOnly.MinValue;
        var checkOut = filter.CheckOut ?? DateOnly.MaxValue;
        var applyAvailability = filter is { CheckIn: not null, CheckOut: not null };
        var applyMinPrice = filter.MinPrice.HasValue;
        var minPrice = filter.MinPrice ?? 0m;
        var applyMaxPrice = filter.MaxPrice.HasValue;
        var maxPrice = filter.MaxPrice ?? 0m;
        var adults = filter.Adults;
        var children = filter.Children;

        // Capacity, availability and price all have to hold for the SAME room, so they live in one
        // Any(...). The availability test and the discount maths are the SQL mirror of Room.IsAvailableFor
        // and Room.GetActivePrice — keep the two in sync if either rule ever changes. The effective-price
        // subquery is inlined (not extracted to a helper) because EF Core only translates in-line
        // expression trees: the lowest price from any discount active on priceDate, else the base price.
        query = query.Where(h => h.Rooms.Any(r =>
            r.IsActive
            && r.AdultCapacity >= adults
            && r.ChildCapacity >= children
            && (!applyAvailability || !r.Availabilities.Any(a =>
                a.Range.StartDate < checkOut && checkIn < a.Range.EndDate))
            && (!applyMinPrice || (r.Discounts
                                       .Where(d => d.IsActive && d.StartDate <= priceDate && priceDate <= d.EndDate)
                                       .Min(d => (decimal?)(d.Type == DiscountType.Percentage
                                           ? r.BasePrice.Amount - r.BasePrice.Amount * d.Value / 100m
                                           : r.BasePrice.Amount - d.Value < 0m
                                               ? 0m
                                               : r.BasePrice.Amount - d.Value))
                                   ?? r.BasePrice.Amount) >= minPrice)
            && (!applyMaxPrice || (r.Discounts
                                       .Where(d => d.IsActive && d.StartDate <= priceDate && priceDate <= d.EndDate)
                                       .Min(d => (decimal?)(d.Type == DiscountType.Percentage
                                           ? r.BasePrice.Amount - r.BasePrice.Amount * d.Value / 100m
                                           : r.BasePrice.Amount - d.Value < 0m
                                               ? 0m
                                               : r.BasePrice.Amount - d.Value))
                                   ?? r.BasePrice.Amount) <= maxPrice)));

        // Rooms + Images come from the specification; Discounts are additionally needed so the handler
        // can compute the displayed nightly price via Room.GetActivePrice.
        query = query.Include(h => h.Rooms).ThenInclude(r => r.Discounts).AsSplitQuery();

        return await PaginateAsync(query, pageNumber, pageSize, cancellationToken);
    }

    public async Task<IReadOnlyList<Hotel>> GetFeaturedDealsAsync(int count,
        CancellationToken cancellationToken = default)
    {
        var today = dateTimeProvider.Today;

        return await context.Hotels
            .AsNoTracking()
            .AsSplitQuery()
            .Where(h => h.ApprovalStatus == HotelApprovalStatus.Approved
                        && h.Rooms.Any(r => r.IsActive
                                            && r.Discounts.Any(d =>
                                                d.IsActive && d.StartDate <= today && today <= d.EndDate)))
            .Include(h => h.City)
            .Include(h => h.Images)
            .Include(h => h.Rooms).ThenInclude(r => r.Discounts)
            .OrderByDescending(h => h.StarRating)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> IsOwnedByAsync(int hotelId, Guid ownerId, CancellationToken cancellationToken = default) =>
        await context.Hotels.AnyAsync(h => h.Id == hotelId && h.OwnerId == ownerId, cancellationToken);

    public async Task<PaginatedList<Hotel>> GetByOwnerIdAsync(Guid ownerId, int pageNumber, int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = context.Hotels
            .AsNoTracking()
            .AsSplitQuery()
            .Where(h => h.OwnerId == ownerId)
            .Include(h => h.City)
            .Include(h => h.Owner)
            .Include(h => h.Rooms)
            .OrderBy(h => h.Name);

        return await PaginateAsync(query, pageNumber, pageSize, cancellationToken);
    }

    public async Task<PaginatedList<Hotel>> GetPendingApprovalAsync(int pageNumber, int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = context.Hotels
            .AsNoTracking()
            .AsSplitQuery()
            .Where(h => h.ApprovalStatus == HotelApprovalStatus.Pending)
            .Include(h => h.City)
            .Include(h => h.Owner)
            .Include(h => h.Rooms)
            .OrderBy(h => h.CreatedAt);

        return await PaginateAsync(query, pageNumber, pageSize, cancellationToken);
    }

    private static async Task<PaginatedList<Hotel>> PaginateAsync(IQueryable<Hotel> query, int pageNumber,
        int pageSize, CancellationToken cancellationToken)
    {
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedList<Hotel>(items, totalCount, pageNumber, pageSize);
    }
}