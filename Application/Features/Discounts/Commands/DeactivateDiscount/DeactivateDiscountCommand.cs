using Application.Common.Security;
using MediatR;

namespace Application.Features.Discounts.Commands.DeactivateDiscount;

[Authorize(Roles = "HotelOwner")]
public record DeactivateDiscountCommand(int DiscountId) : IRequest;