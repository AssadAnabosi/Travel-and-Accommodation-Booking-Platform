using Application.Common.Exceptions;
using Application.Common.Interfaces.Persistence;
using Application.Common.Models;
using Application.Features.Reviews.Common;
using Domain.Entities;
using MediatR;

namespace Application.Features.Reviews.Queries.GetReviewsByHotel;

public class GetReviewsByHotelQueryHandler(IHotelRepository hotelRepository, IReviewRepository reviewRepository)    
    : IRequestHandler<GetReviewsByHotelQuery, PaginatedList<ReviewDto>>
{
    public async Task<PaginatedList<ReviewDto>> Handle(GetReviewsByHotelQuery request,
        CancellationToken cancellationToken)
    {
        _ = await hotelRepository.GetByIdAsync(request.HotelId, cancellationToken)
            ?? throw new NotFoundException(nameof(Hotel), request.HotelId);

        var result = await reviewRepository.GetByHotelIdAsync(request.HotelId, request.PageNumber, request.PageSize,
            cancellationToken);

        var items = result.Items
            .Select(r => new ReviewDto(r.Id, r.HotelId, r.UserId, $"{r.User.FirstName} {r.User.LastName}", r.Rating,
                r.Comment, r.CreatedAt, r.ModifiedAt))
            .ToList();

        return new PaginatedList<ReviewDto>(items, result.TotalCount, result.PageNumber, request.PageSize);
    }
}