using Application.Common.Models;
using Domain.Entities;

namespace Application.Common.Interfaces.Persistence;

public interface IReviewRepository : IRepository<Review, int>
{
    Task<PaginatedList<Review>> GetByHotelIdAsync(int hotelId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<bool> UserHasReviewedAsync(Guid userId, int hotelId, CancellationToken cancellationToken = default);
}