using System.Net;
using Application.Common.Exceptions;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.Common.Models;
using Domain.Entities;
using MediatR;

namespace Application.Features.Hotels.Commands.ApproveHotel;

public class ApproveHotelCommandHandler(
    IHotelRepository hotelRepository,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    IEmailService emailService)
    : IRequestHandler<ApproveHotelCommand>
{
    public async Task Handle(ApproveHotelCommand request, CancellationToken cancellationToken)
    {
        var hotel = await hotelRepository.GetByIdAsync(request.HotelId, cancellationToken)
                    ?? throw new NotFoundException(nameof(Hotel), request.HotelId);

        hotel.Approve();

        hotelRepository.Update(hotel);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Notify the owner their hotel went live (best-effort; see IEmailService).
        var owner = await userRepository.GetByIdAsync(hotel.OwnerId, cancellationToken);
        if (owner is null) return;

        var emailBody =
            $"<p>Hi {WebUtility.HtmlEncode(owner.FirstName)},</p>" +
            $"<p>Good news: your hotel <strong>{WebUtility.HtmlEncode(hotel.Name)}</strong> has been approved " +
            "and is now visible to guests in search.</p>";

        await emailService.SendAsync(
            new EmailMessage(owner.Email, $"Your hotel \"{hotel.Name}\" is live", emailBody),
            cancellationToken);
    }
}
