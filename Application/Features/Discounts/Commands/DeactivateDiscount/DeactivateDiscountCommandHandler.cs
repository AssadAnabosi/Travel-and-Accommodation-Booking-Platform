using Application.Common.Exceptions;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Domain.Entities;
using MediatR;

namespace Application.Features.Discounts.Commands.DeactivateDiscount;

public class DeactivateDiscountCommandHandler(
    IDiscountRepository discountRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService)
    : IRequestHandler<DeactivateDiscountCommand>
{
    public async Task Handle(DeactivateDiscountCommand request, CancellationToken cancellationToken)
    {
        var discount = await discountRepository.GetByIdWithRoomAsync(request.DiscountId, cancellationToken)
                       ?? throw new NotFoundException(nameof(Discount), request.DiscountId);

        if (discount.Room.Hotel.OwnerId != currentUserService.UserId)
            throw new ForbiddenAccessException("You can only deactivate discounts for your own hotel's rooms.");

        discount.Deactivate();

        discountRepository.Update(discount);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}