using Application.Common.Models;
using Application.Features.Reviews.Common;
using MediatR;

namespace Application.Features.Reviews.Queries.GetReviewsByHotel;

public record GetReviewsByHotelQuery(int HotelId, int PageNumber = 1, int PageSize = 20)
    : IRequest<PaginatedList<ReviewDto>>;