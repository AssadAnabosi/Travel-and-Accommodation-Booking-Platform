using Application.Common.Security;
using MediatR;

namespace Application.Features.Hotels.Commands.AddHotelImage;

[Authorize(Roles = "Admin,HotelOwner")]
public record AddHotelImageCommand(int HotelId, string Url) : IRequest<int>; // returns new HotelImage.Id