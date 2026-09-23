using Application.Common.Models;
using Application.Common.Security;
using MediatR;

namespace Application.Features.Rooms.Queries.GetRoomImages;

[Authorize(Roles = "Admin,HotelOwner")]
public record GetRoomImagesQuery(int RoomId) : IRequest<IReadOnlyList<ImageDto>>;
