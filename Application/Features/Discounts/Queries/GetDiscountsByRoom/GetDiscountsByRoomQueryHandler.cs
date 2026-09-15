using Application.Common.Exceptions;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.Features.Discounts.Common;
using Domain.Entities;
using MediatR;

namespace Application.Features.Discounts.Queries.GetDiscountsByRoom;

public class GetDiscountsByRoomQueryHandler(
    IRoomRepository roomRepository,
    IDiscountRepository discountRepository,
    ICurrentUserService currentUserService)
    : IRequestHandler<GetDiscountsByRoomQuery, IReadOnlyList<DiscountDto>>
{
    public async Task<IReadOnlyList<DiscountDto>> Handle(GetDiscountsByRoomQuery request,
        CancellationToken cancellationToken)
    {
        var room = await roomRepository.GetByIdWithDetailsAsync(request.RoomId, cancellationToken)
                   ?? throw new NotFoundException(nameof(Room), request.RoomId);

        if (!currentUserService.IsInRole("Admin") && room.Hotel.OwnerId != currentUserService.UserId)
            throw new ForbiddenAccessException("You can only view discounts for your own hotel's rooms.");

        var discounts = await discountRepository.GetByRoomIdAsync(request.RoomId, cancellationToken);

        return discounts.Select(d => new DiscountDto(d.Id, d.RoomId, d.Name, d.Type.ToString(), d.Value,
            d.StartDate, d.EndDate, d.IsActive, d.CreatedAt, d.ModifiedAt)).ToList();
    }
}