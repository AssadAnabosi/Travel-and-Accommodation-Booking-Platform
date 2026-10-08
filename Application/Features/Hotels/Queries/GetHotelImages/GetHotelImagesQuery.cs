using Application.Common.Models;
using Application.Common.Security;
using MediatR;

namespace Application.Features.Hotels.Queries.GetHotelImages;

[Authorize(Roles = "Admin,HotelOwner")]
public record GetHotelImagesQuery(int HotelId) : IRequest<IReadOnlyList<ImageDto>>;
