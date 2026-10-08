using Application.Common.Exceptions;
using Application.Common.Interfaces.Persistence;
using Domain.Entities;
using MediatR;

namespace Application.Features.Hotels.Commands.ReassignHotelOwner;

public class ReassignHotelOwnerCommandHandler(IHotelRepository hotelRepository, IUnitOfWork unitOfWork)
    : IRequestHandler<ReassignHotelOwnerCommand>
{
    public async Task Handle(ReassignHotelOwnerCommand request, CancellationToken cancellationToken)
    {
        var hotel = await hotelRepository.GetByIdAsync(request.HotelId, cancellationToken)
                    ?? throw new NotFoundException(nameof(Hotel), request.HotelId);

        // Idempotent: reassigning to the current owner is a no-op rather than an error.
        if (hotel.OwnerId == request.NewOwnerId) return;

        hotel.ReassignOwner(request.NewOwnerId);

        hotelRepository.Update(hotel);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
