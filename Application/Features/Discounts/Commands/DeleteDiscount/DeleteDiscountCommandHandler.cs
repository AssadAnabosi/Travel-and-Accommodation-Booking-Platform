using Application.Common.Exceptions;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Domain.Entities;
using MediatR;

namespace Application.Features.Discounts.Commands.DeleteDiscount;

public class DeleteDiscountCommandHandler(
    IDiscountRepository discountRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService)
    : IRequestHandler<DeleteDiscountCommand>
{
    public async Task Handle(DeleteDiscountCommand request, CancellationToken cancellationToken)
    {
        var discount = await discountRepository.GetByIdWithRoomAsync(request.DiscountId, cancellationToken)
                       ?? throw new NotFoundException(nameof(Discount), request.DiscountId);

        if (discount.Room.Hotel.OwnerId != currentUserService.UserId)
            throw new ForbiddenAccessException("You can only delete discounts for your own hotel's rooms.");

        discountRepository.Remove(discount);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}