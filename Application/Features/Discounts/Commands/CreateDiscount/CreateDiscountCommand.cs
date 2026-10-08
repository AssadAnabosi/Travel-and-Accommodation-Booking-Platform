using Application.Common.Security;
using Application.Features.Discounts.Common;
using Domain.Enums;
using MediatR;

namespace Application.Features.Discounts.Commands.CreateDiscount;

[Authorize(Roles = "HotelOwner")]
public record CreateDiscountCommand(
    int RoomId,
    string Name,
    DiscountType Type,
    decimal Value,
    DateOnly StartDate,
    DateOnly EndDate) : IRequest<DiscountDto>;