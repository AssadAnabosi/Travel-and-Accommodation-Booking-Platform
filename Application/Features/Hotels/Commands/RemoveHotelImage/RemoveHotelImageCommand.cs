using Application.Common.Security;
using MediatR;

namespace Application.Features.Hotels.Commands.RemoveHotelImage;

[Authorize(Roles = "Admin,HotelOwner")]
public record RemoveHotelImageCommand(int HotelId, int ImageId) : IRequest;