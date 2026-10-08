using Domain.Common;
using Domain.Exceptions;

namespace Domain.ValueObjects;

public sealed class DateRange : ValueObject
{
    public DateOnly StartDate { get; }
    public DateOnly EndDate { get; }

    private DateRange(DateOnly startDate, DateOnly endDate)
    {
        StartDate = startDate;
        EndDate = endDate;
    }

    public static DateRange Of(DateOnly startDate, DateOnly endDate)
    {
        if (endDate <= startDate)
            throw new InvalidDateRangeException("End date must be after the start date.");

        return new DateRange(startDate, endDate);
    }

    public int Nights => EndDate.DayNumber - StartDate.DayNumber;

    public bool Overlaps(DateRange other) =>
        StartDate < other.EndDate && other.StartDate < EndDate;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return StartDate;
        yield return EndDate;
    }

    public override string ToString() => $"{StartDate:yyyy-MM-dd} → {EndDate:yyyy-MM-dd}";
}