using Application.Common.Models;
using Application.Common.Security;
using Application.Features.Hotels.Common;
using MediatR;

namespace Application.Features.Hotels.Queries.GetPendingHotels;

[Authorize(Roles = "Admin")]
public record GetPendingHotelsQuery(int PageNumber = 1, int PageSize = 20) : IRequest<PaginatedList<HotelDto>>;