using Application.Common.Security;
using MediatR;

namespace Application.Features.Hotels.Commands.DeleteHotel;

[Authorize(Roles = "Admin")]
public record DeleteHotelCommand(int HotelId) : IRequest;