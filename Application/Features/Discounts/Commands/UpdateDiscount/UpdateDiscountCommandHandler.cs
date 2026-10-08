using Application.Common.Exceptions;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.Features.Discounts.Common;
using Domain.Entities;
using MediatR;

namespace Application.Features.Discounts.Commands.UpdateDiscount;

public class UpdateDiscountCommandHandler(
    IDiscountRepository discountRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService)
    : IRequestHandler<UpdateDiscountCommand, DiscountDto>
{
    public async Task<DiscountDto> Handle(UpdateDiscountCommand request, CancellationToken cancellationToken)
    {
        var discount = await discountRepository.GetByIdWithRoomAsync(request.DiscountId, cancellationToken)
                       ?? throw new NotFoundException(nameof(Discount), request.DiscountId);

        if (discount.Room.Hotel.OwnerId != currentUserService.UserId)
            throw new ForbiddenAccessException("You can only update discounts for your own hotel's rooms.");

        discount.Update(request.Name, request.Type, request.Value, request.StartDate, request.EndDate);

        discountRepository.Update(discount);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new DiscountDto(discount.Id, discount.RoomId, discount.Name, discount.Type.ToString(), discount.Value,
            discount.StartDate, discount.EndDate, discount.IsActive, discount.CreatedAt, discount.ModifiedAt);
    }
}