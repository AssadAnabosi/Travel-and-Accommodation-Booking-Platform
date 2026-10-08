using Application.Common.Security;
using Application.Features.Hotels.Common;
using MediatR;

namespace Application.Features.Hotels.Commands.CreateHotel;

[Authorize(Roles = "Admin,HotelOwner")]
public record CreateHotelCommand(
    string Name,
    int StarRating,
    string Description,
    string Address,
    double Latitude,
    double Longitude,
    int CityId,
    Guid? OwnerId) : IRequest<HotelDto>;