using Domain.Entities;

namespace Application.Common.Interfaces.Persistence;

public interface IDiscountRepository : IRepository<Discount, int>
{
    Task<IReadOnlyList<Discount>> GetActiveByRoomIdAsync(int roomId, DateOnly onDate, CancellationToken cancellationToken = default);
}