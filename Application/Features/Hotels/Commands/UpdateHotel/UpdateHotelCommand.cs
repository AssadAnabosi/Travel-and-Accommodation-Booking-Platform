using Application.Common.Security;
using Application.Features.Hotels.Common;
using MediatR;

namespace Application.Features.Hotels.Commands.UpdateHotel;

[Authorize(Roles = "Admin,HotelOwner")]
public record UpdateHotelCommand(
    int HotelId,
    string Name,
    int StarRating,
    string Description,
    string Address,
    double Latitude,
    double Longitude,
    int CityId) : IRequest<HotelDto>;