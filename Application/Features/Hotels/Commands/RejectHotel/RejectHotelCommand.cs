using Application.Common.Security;
using MediatR;

namespace Application.Features.Hotels.Commands.RejectHotel;

[Authorize(Roles = "Admin")]
public record RejectHotelCommand(int HotelId, string Reason) : IRequest;