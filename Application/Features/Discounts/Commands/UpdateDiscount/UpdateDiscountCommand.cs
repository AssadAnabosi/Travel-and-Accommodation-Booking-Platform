using Application.Common.Security;
using Application.Features.Discounts.Common;
using Domain.Enums;
using MediatR;

namespace Application.Features.Discounts.Commands.UpdateDiscount;

[Authorize(Roles = "HotelOwner")]
public record UpdateDiscountCommand(
    int DiscountId,
    string Name,
    DiscountType Type,
    decimal Value,
    DateOnly StartDate,
    DateOnly EndDate) : IRequest<DiscountDto>;