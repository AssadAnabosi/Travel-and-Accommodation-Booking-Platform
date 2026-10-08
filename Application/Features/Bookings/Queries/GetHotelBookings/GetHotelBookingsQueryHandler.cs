using Application.Common.Exceptions;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.Common.Models;
using Application.Features.Bookings.Common;
using Domain.Entities;
using MediatR;

namespace Application.Features.Bookings.Queries.GetHotelBookings;

public class GetHotelBookingsQueryHandler(
    IHotelRepository hotelRepository,
    IBookingRepository bookingRepository,
    ICurrentUserService currentUserService)
    : IRequestHandler<GetHotelBookingsQuery, PaginatedList<HotelBookingListItemDto>>
{
    public async Task<PaginatedList<HotelBookingListItemDto>> Handle(GetHotelBookingsQuery request,
        CancellationToken cancellationToken)
    {
        var hotel = await hotelRepository.GetByIdAsync(request.HotelId, cancellationToken)
                    ?? throw new NotFoundException(nameof(Hotel), request.HotelId);

        if (!currentUserService.IsInRole("Admin") && hotel.OwnerId != currentUserService.UserId)
            throw new ForbiddenAccessException("You can only view bookings for your own hotel.");

        var filter = new HotelBookingFilter(request.Status, request.CheckInFrom, request.CheckInTo, request.Keyword);
        var result = await bookingRepository.GetByHotelIdAsync(request.HotelId, filter, request.PageNumber,
            request.PageSize, cancellationToken);

        var items = result.Items.Select(b => new HotelBookingListItemDto(
            b.Id, b.ConfirmationNumber, b.UserId, $"{b.User.FirstName} {b.User.LastName}", b.User.Email,
            b.RoomId, b.Room.Number, b.StayRange.StartDate, b.StayRange.EndDate, b.Adults, b.Children,
            b.Status.ToString(), b.TotalPrice.Amount, b.TotalPrice.Currency, b.SpecialRequests,
            b.CreatedAt)).ToList();

        return new PaginatedList<HotelBookingListItemDto>(items, result.TotalCount, result.PageNumber,
            request.PageSize);
    }
}
