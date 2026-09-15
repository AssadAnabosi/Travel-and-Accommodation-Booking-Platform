using Application.Common.Exceptions;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Domain.Entities;
using MediatR;

namespace Application.Features.HotelVisits.Commands.RecordHotelVisit;

public class RecordHotelVisitCommandHandler(
    IHotelRepository hotelRepository,
    IHotelVisitRepository hotelVisitRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService)
    : IRequestHandler<RecordHotelVisitCommand>
{
    public async Task Handle(RecordHotelVisitCommand request, CancellationToken cancellationToken)
    {
        _ = await hotelRepository.GetByIdAsync(request.HotelId, cancellationToken)
            ?? throw new NotFoundException(nameof(Hotel), request.HotelId);

        // UserId is null for anonymous callers — intentional, both count toward trending cities.
        var visit = HotelVisit.Record(currentUserService.UserId, request.HotelId);

        await hotelVisitRepository.RecordAsync(visit, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}