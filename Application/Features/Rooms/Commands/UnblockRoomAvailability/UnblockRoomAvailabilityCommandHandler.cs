using Application.Common.Exceptions;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Domain.Entities;
using MediatR;

namespace Application.Features.Rooms.Commands.UnblockRoomAvailability;

public class UnblockRoomAvailabilityCommandHandler(
    IRoomRepository roomRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService)
    : IRequestHandler<UnblockRoomAvailabilityCommand>
{
    public async Task Handle(UnblockRoomAvailabilityCommand request, CancellationToken cancellationToken)
    {
        var room = await roomRepository.GetByIdWithDetailsAsync(request.RoomId, cancellationToken)
                   ?? throw new NotFoundException(nameof(Room), request.RoomId);

        if (!currentUserService.IsInRole("Admin") && room.Hotel.OwnerId != currentUserService.UserId)
            throw new ForbiddenAccessException("You can only manage availability for your own hotel's rooms.");

        var availability = room.FindAvailability(request.AvailabilityId)
                           ?? throw new NotFoundException("RoomAvailability", request.AvailabilityId);

        room.Unblock(availability);

        roomRepository.Update(room);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}