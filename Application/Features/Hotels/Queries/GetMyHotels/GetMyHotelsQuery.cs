using Application.Common.Models;
using Application.Common.Security;
using Application.Features.Hotels.Common;
using MediatR;

namespace Application.Features.Hotels.Queries.GetMyHotels;

[Authorize(Roles = "HotelOwner")]
public record GetMyHotelsQuery(int PageNumber = 1, int PageSize = 20) : IRequest<PaginatedList<HotelDto>>;