using Application.Common.Security;
using MediatR;

namespace Application.Features.Hotels.Commands.UpdateHotelAmenities;

[Authorize(Roles = "Admin,HotelOwner")]
public record UpdateHotelAmenitiesCommand(int HotelId, IReadOnlyList<int> AmenityIds) : IRequest;