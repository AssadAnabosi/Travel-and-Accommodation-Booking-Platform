using Application.Features.Hotels.Common;
using MediatR;

namespace Application.Features.Hotels.Queries.GetHotelDetail;

public record GetHotelDetailQuery(int HotelId, DateOnly? CheckIn, DateOnly? CheckOut) : IRequest<HotelDetailDto>;