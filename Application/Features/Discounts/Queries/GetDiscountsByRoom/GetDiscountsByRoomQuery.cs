using Application.Common.Security;
using Application.Features.Discounts.Common;
using MediatR;

namespace Application.Features.Discounts.Queries.GetDiscountsByRoom;

[Authorize(Roles = "Admin,HotelOwner")]
public record GetDiscountsByRoomQuery(int RoomId) : IRequest<IReadOnlyList<DiscountDto>>;