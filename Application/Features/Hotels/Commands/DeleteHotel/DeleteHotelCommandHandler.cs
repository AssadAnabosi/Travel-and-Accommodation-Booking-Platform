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

        if (hotel.Rooms.Count > 0)
            throw new ConflictException("Cannot delete a hotel that still has rooms. Remove its rooms first.");

        hotelRepository.Remove(hotel);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}