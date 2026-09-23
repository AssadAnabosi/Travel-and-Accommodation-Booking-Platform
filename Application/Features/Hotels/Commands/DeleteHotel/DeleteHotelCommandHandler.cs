using Application.Common.Exceptions;
using Application.Common.Interfaces.Persistence;
using Domain.Entities;
using MediatR;

namespace Application.Features.Hotels.Commands.DeleteHotel;

public class DeleteHotelCommandHandler(IHotelRepository hotelRepository, IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteHotelCommand>
{
    public async Task Handle(DeleteHotelCommand request, CancellationToken cancellationToken)
    {
        var hotel = await hotelRepository.GetByIdAsync(request.HotelId, cancellationToken)
                    ?? throw new NotFoundException(nameof(Hotel), request.HotelId);

        // Query, not hotel.Rooms: GetByIdAsync doesn't load Rooms, so that count was always 0 and the
        // Cascade FK silently deleted every room (decision #72). Soft-deleted rooms count — they hold history.
        if (await hotelRepository.HasRoomsAsync(hotel.Id, cancellationToken))
            throw new ConflictException("Cannot delete a hotel that still has rooms. Remove its rooms first.");

        hotelRepository.Remove(hotel);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}