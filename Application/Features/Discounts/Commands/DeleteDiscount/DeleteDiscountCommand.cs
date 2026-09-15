using Application.Common.Security;
using MediatR;

namespace Application.Features.Discounts.Commands.DeleteDiscount;

[Authorize(Roles = "HotelOwner")]
public record DeleteDiscountCommand(int DiscountId) : IRequest;