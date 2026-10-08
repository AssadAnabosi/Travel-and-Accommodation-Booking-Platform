using Application.Common.Exceptions;
using Application.Common.Interfaces.Persistence;
using Domain.Entities;
using MediatR;

namespace Application.Features.Hotels.Commands.RejectHotel;

public class RejectHotelCommandHandler(IHotelRepository hotelRepository, IUnitOfWork unitOfWork)
    : IRequestHandler<RejectHotelCommand>
{
    public async Task Handle(RejectHotelCommand request, CancellationToken cancellationToken)
    {
        var hotel = await hotelRepository.GetByIdAsync(request.HotelId, cancellationToken)
                    ?? throw new NotFoundException(nameof(Hotel), request.HotelId);

        hotel.Reject(request.Reason);

        hotelRepository.Update(hotel);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // TODO once IEmailService exists: notify the owner with the rejection reason.
    }
}