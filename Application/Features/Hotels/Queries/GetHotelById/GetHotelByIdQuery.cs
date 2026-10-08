using Application.Common.Security;
using Application.Features.Hotels.Common;
using MediatR;

namespace Application.Features.Hotels.Queries.GetHotelById;

[Authorize(Roles = "Admin,HotelOwner")]
public record GetHotelByIdQuery(int HotelId) : IRequest<HotelDto>;