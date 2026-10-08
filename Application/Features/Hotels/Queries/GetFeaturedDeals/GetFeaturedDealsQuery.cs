using Application.Features.Hotels.Common;
using MediatR;

namespace Application.Features.Hotels.Queries.GetFeaturedDeals;

public record GetFeaturedDealsQuery(int Count = 5) : IRequest<IReadOnlyList<FeaturedDealDto>>;