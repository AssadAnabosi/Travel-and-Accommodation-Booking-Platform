using Application.Common.Models;
using Domain.Entities;

namespace Application.Common.Interfaces.Persistence;

public interface IBookingRepository : IRepository<Booking, Guid>
{
    Task<Booking?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PaginatedList<Booking>> GetByUserIdAsync(Guid userId, int pageNumber, int pageSize,
        CancellationToken cancellationToken = default);
}