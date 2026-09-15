using Application.Common.Security;
using Application.Features.HotelVisits.Common;
using MediatR;

namespace Application.Features.HotelVisits.Queries.GetRecentlyVisited;

[Authorize]
public record GetRecentlyVisitedQuery(int Count = 5) : IRequest<IReadOnlyList<RecentlyVisitedDto>>;