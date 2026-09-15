using Application.Common.Security;
using Application.Features.Hotels.Common;
using MediatR;

namespace Application.Features.Hotels.Commands.CreateHotel;

// OwnerId is only meaningful when the caller is Admin — ignored/rejected otherwise (see validator).
[Authorize(Roles = "Admin,HotelOwner")]
public record CreateHotelCommand(string Name, int StarRating, string Description, int CityId, Guid? OwnerId)
    : IRequest<HotelDto>;