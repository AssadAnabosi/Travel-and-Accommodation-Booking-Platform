using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.Features.Hotels.Common;
using Domain.Entities;
using MediatR;

namespace Application.Features.Hotels.Commands.CreateHotel;

public class CreateHotelCommandHandler(
    IHotelRepository hotelRepository,
    ICityRepository cityRepository,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService)
    : IRequestHandler<CreateHotelCommand, HotelDto>
{
    public async Task<HotelDto> Handle(CreateHotelCommand request, CancellationToken cancellationToken)
    {
        var isAdmin = currentUserService.IsInRole("Admin");
        var callerId = currentUserService.UserId!.Value;

        var hotel = isAdmin
            ? Hotel.CreateByAdmin(request.Name, request.StarRating, request.Description, request.Address,
                request.Latitude, request.Longitude, request.CityId, request.OwnerId!.Value)
            : Hotel.CreateByOwner(request.Name, request.StarRating, request.Description, request.Address,
                request.Latitude, request.Longitude, request.CityId, callerId);

        await hotelRepository.AddAsync(hotel, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var city = await cityRepository.GetByIdAsync(hotel.CityId, cancellationToken);
        var owner = await userRepository.GetByIdAsync(hotel.OwnerId, cancellationToken);

        return new HotelDto(hotel.Id, hotel.Name, hotel.StarRating, hotel.Description, hotel.Address, hotel.Latitude,
            hotel.Longitude, hotel.CityId, city!.Name,
            hotel.OwnerId, $"{owner!.FirstName} {owner.LastName}", hotel.ApprovalStatus.ToString(),
            hotel.RejectionReason,
            RoomsCount: 0, hotel.CreatedAt, hotel.ModifiedAt);
    }
}