using Application.Common.Exceptions;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.Features.Hotels.Common;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Features.Hotels.Commands.UpdateHotel;

public class UpdateHotelCommandHandler(
    IHotelRepository hotelRepository,
    ICityRepository cityRepository,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService)
    : IRequestHandler<UpdateHotelCommand, HotelDto>
{
    public async Task<HotelDto> Handle(UpdateHotelCommand request, CancellationToken cancellationToken)
    {
        // Amenity links are loaded so the response's AmenityIds reflects the hotel (the update itself doesn't touch them).
        var hotel = await hotelRepository.GetByIdWithAmenitiesTrackedAsync(request.HotelId, cancellationToken)
                    ?? throw new NotFoundException(nameof(Hotel), request.HotelId);

        var isAdmin = currentUserService.IsInRole("Admin");
        if (!isAdmin && hotel.OwnerId != currentUserService.UserId)
            throw new ForbiddenAccessException("You can only update your own hotel.");

        var wasRejected = hotel.ApprovalStatus == HotelApprovalStatus.Rejected;

        hotel.Update(request.Name, request.StarRating, request.Description, request.Address, request.Latitude,
            request.Longitude, request.CityId);

        // Owner editing a rejected listing re-enters the review queue automatically.
        // Admin edits don't trigger this — an Admin can just Approve directly if needed.
        if (!isAdmin && wasRejected)
            hotel.Resubmit();

        hotelRepository.Update(hotel);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var city = await cityRepository.GetByIdAsync(hotel.CityId, cancellationToken);
        var owner = await userRepository.GetByIdAsync(hotel.OwnerId, cancellationToken);
        // Rooms isn't loaded here (hotel.Rooms.Count would always be 0), so count them in the database.
        var roomsCount = await hotelRepository.CountRoomsAsync(hotel.Id, cancellationToken);

        return new HotelDto(hotel.Id, hotel.Name, hotel.StarRating, hotel.Description, hotel.Address, hotel.Latitude,
            hotel.Longitude, hotel.CityId, city!.Name,
            hotel.OwnerId, $"{owner!.FirstName} {owner.LastName}", hotel.ApprovalStatus.ToString(),
            hotel.RejectionReason,
            roomsCount, hotel.CreatedAt, hotel.ModifiedAt, hotel.HotelAmenities.Select(ha => ha.AmenityId).ToList());
    }
}