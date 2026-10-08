using Application.Common.Exceptions;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.Features.Discounts.Common;
using Domain.Entities;
using MediatR;

namespace Application.Features.Discounts.Commands.CreateDiscount;

public class CreateDiscountCommandHandler(
    IRoomRepository roomRepository,
    IDiscountRepository discountRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService)
    : IRequestHandler<CreateDiscountCommand, DiscountDto>
{
    public async Task<DiscountDto> Handle(CreateDiscountCommand request, CancellationToken cancellationToken)
    {
        var room = await roomRepository.GetByIdWithDetailsAsync(request.RoomId, cancellationToken)
                   ?? throw new NotFoundException(nameof(Room), request.RoomId);

        if (room.Hotel.OwnerId != currentUserService.UserId)
            throw new ForbiddenAccessException("You can only create discounts for your own hotel's rooms.");

        var discount = Discount.Create(request.RoomId, request.Name, request.Type, request.Value, request.StartDate,
            request.EndDate);

        await discountRepository.AddAsync(discount, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new DiscountDto(discount.Id, discount.RoomId, discount.Name, discount.Type.ToString(), discount.Value,
            discount.StartDate, discount.EndDate, discount.IsActive, discount.CreatedAt, discount.ModifiedAt);
    }
}