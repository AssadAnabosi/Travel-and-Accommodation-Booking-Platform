using Application.Common.Security;
using MediatR;

namespace Application.Features.Hotels.Commands.ApproveHotel;

[Authorize(Roles = "Admin")]
public record ApproveHotelCommand(int HotelId) : IRequest;