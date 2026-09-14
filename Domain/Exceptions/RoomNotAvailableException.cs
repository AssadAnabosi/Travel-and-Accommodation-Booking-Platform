using Domain.ValueObjects;

namespace Domain.Exceptions;

public sealed class RoomNotAvailableException(int roomId, DateRange range)
    : DomainException($"Room {roomId} is not available for the range {range}.");