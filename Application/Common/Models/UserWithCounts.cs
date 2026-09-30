using Domain.Entities;

namespace Application.Common.Models;

public record UserWithCounts(User User, int OwnedHotelsCount, int BookingsCount);
