using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.Common.Models;
using Application.Features.Bookings.Common;
using MediatR;

namespace Application.Features.Bookings.Queries.GetMyBookings;

public class GetMyBookingsQueryHandler(IBookingRepository bookingRepository, ICurrentUserService currentUserService)
    : IRequestHandler<GetMyBookingsQuery, PaginatedList<BookingListItemDto>>
{
    public async Task<PaginatedList<BookingListItemDto>> Handle(GetMyBookingsQuery request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId!.Value;
        var result =
            await bookingRepository.GetByUserIdAsync(userId, request.PageNumber, request.PageSize, cancellationToken);

        var items = result.Items.Select(b => new BookingListItemDto(
            b.Id, b.ConfirmationNumber, b.Room.Hotel.Name, b.Room.Number,
            b.StayRange.StartDate, b.StayRange.EndDate, b.Status.ToString(), b.TotalPrice.Amount,
            b.TotalPrice.Currency)).ToList();

        return new PaginatedList<BookingListItemDto>(items, result.TotalCount, result.PageNumber, request.PageSize);
    }
}