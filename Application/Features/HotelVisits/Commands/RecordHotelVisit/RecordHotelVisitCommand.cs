using MediatR;

namespace Application.Features.HotelVisits.Commands.RecordHotelVisit;

public record RecordHotelVisitCommand(int HotelId) : IRequest;