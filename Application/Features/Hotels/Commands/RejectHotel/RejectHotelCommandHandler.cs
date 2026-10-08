using System.Net;
using Application.Common.Exceptions;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.Common.Models;
using Domain.Entities;
using MediatR;

namespace Application.Features.Hotels.Commands.RejectHotel;

public class RejectHotelCommandHandler(
    IHotelRepository hotelRepository,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    IEmailService emailService)
    : IRequestHandler<RejectHotelCommand>
{
    public async Task Handle(RejectHotelCommand request, CancellationToken cancellationToken)
    {
        var hotel = await hotelRepository.GetByIdAsync(request.HotelId, cancellationToken)
                    ?? throw new NotFoundException(nameof(Hotel), request.HotelId);

        hotel.Reject(request.Reason);

        hotelRepository.Update(hotel);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Notify the owner with the reason (best-effort; see IEmailService).
        var owner = await userRepository.GetByIdAsync(hotel.OwnerId, cancellationToken);
        if (owner is null) return;

        var emailBody =
            $"<p>Hi {WebUtility.HtmlEncode(owner.FirstName)},</p>" +
            $"<p>Your hotel <strong>{WebUtility.HtmlEncode(hotel.Name)}</strong> was not approved.</p>" +
            $"<p>Reason: {WebUtility.HtmlEncode(hotel.RejectionReason)}</p>" +
            "<p>Edit the listing to address this and it will be resubmitted for review automatically.</p>";

        await emailService.SendAsync(
            new EmailMessage(owner.Email, $"Your hotel \"{hotel.Name}\" needs changes", emailBody),
            cancellationToken);
    }
}
