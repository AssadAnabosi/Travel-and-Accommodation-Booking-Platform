using Application.Common.Exceptions;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Domain.Entities;
using Domain.ValueObjects;
using MediatR;

namespace Application.Features.Rooms.Commands.BlockRoomAvailability;

public class BlockRoomAvailabilityCommandHandler(
    IRoomRepository roomRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService)
    : IRequestHandler<BlockRoomAvailabilityCommand, int>
{
    public async Task<int> Handle(BlockRoomAvailabilityCommand request, CancellationToken cancellationToken)
    {
        var room = await roomRepository.GetByIdWithDetailsAsync(request.RoomId, cancellationToken)
                   ?? throw new NotFoundException(nameof(Room), request.RoomId);

        if (!currentUserService.IsInRole("Admin") && room.Hotel.OwnerId != currentUserService.UserId)
            throw new ForbiddenAccessException("You can only manage availability for your own hotel's rooms.");

        var range = DateRange.Of(request.StartDate, request.EndDate);
        var availability =
            room.Block(range); // throws RoomNotAvailableException if it overlaps a booking or existing block

        roomRepository.Update(room);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return availability.Id;
    }
}