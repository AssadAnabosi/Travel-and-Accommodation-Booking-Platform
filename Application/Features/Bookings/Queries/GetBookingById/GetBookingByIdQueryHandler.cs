using Application.Common.Exceptions;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.Features.Bookings.Common;
using Domain.Entities;
using MediatR;

namespace Application.Features.Bookings.Queries.GetBookingById;

public class GetBookingByIdQueryHandler(IBookingRepository bookingRepository, ICurrentUserService currentUserService)
    : IRequestHandler<GetBookingByIdQuery, BookingDetailDto>
{
    public async Task<BookingDetailDto> Handle(GetBookingByIdQuery request, CancellationToken cancellationToken)
    {
        var booking = await bookingRepository.GetByIdWithDetailsAsync(request.BookingId, cancellationToken)
                      ?? throw new NotFoundException(nameof(Booking), request.BookingId);

        var isOwnerOfBooking = booking.UserId == currentUserService.UserId;
        var isAdmin = currentUserService.IsInRole("Admin");
        var isHotelOwner = booking.Room.Hotel.OwnerId == currentUserService.UserId;

        if (!isOwnerOfBooking && !isAdmin && !isHotelOwner)
            throw new ForbiddenAccessException("You do not have access to this booking.");

        return new BookingDetailDto(
            booking.Id, booking.ConfirmationNumber, booking.Status.ToString(),
            booking.Room.Hotel.Name, booking.Room.Hotel.Address, booking.Room.Number, booking.Room.RoomType.ToString(),
            booking.StayRange.StartDate, booking.StayRange.EndDate, booking.StayRange.Nights,
            booking.Adults, booking.Children, booking.TotalPrice.Amount, booking.TotalPrice.Currency,
            booking.SpecialRequests, booking.CreatedAt);
    }
}