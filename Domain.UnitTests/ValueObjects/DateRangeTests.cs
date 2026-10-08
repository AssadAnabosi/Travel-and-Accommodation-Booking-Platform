using Domain.Exceptions;
using Domain.ValueObjects;
using FluentAssertions;

namespace Domain.UnitTests.ValueObjects;

public class DateRangeTests
{
    private static DateOnly D(int day) => new(2026, 1, day);

    [Fact]
    public void Of_ValidRange_SetsDatesAndNights()
    {
        var range = DateRange.Of(D(10), D(15));

        range.StartDate.Should().Be(D(10));
        range.EndDate.Should().Be(D(15));
        range.Nights.Should().Be(5);
    }

    [Fact]
    public void Of_EndEqualsStart_Throws()
    {
        var act = () => DateRange.Of(D(10), D(10));

        act.Should().Throw<InvalidDateRangeException>();
    }

    [Fact]
    public void Of_EndBeforeStart_Throws()
    {
        var act = () => DateRange.Of(D(15), D(10));

        act.Should().Throw<InvalidDateRangeException>();
    }

    [Fact]
    public void Overlaps_OverlappingRanges_ReturnsTrue()
    {
        var a = DateRange.Of(D(10), D(15));
        var b = DateRange.Of(D(14), D(20));

        a.Overlaps(b).Should().BeTrue();
        b.Overlaps(a).Should().BeTrue();
    }

    [Fact]
    public void Overlaps_AdjacentRanges_ReturnsFalse()
    {
        // Half-open semantics: one range ending exactly when the next starts do not overlap.
        var a = DateRange.Of(D(10), D(15));
        var b = DateRange.Of(D(15), D(20));

        a.Overlaps(b).Should().BeFalse();
        b.Overlaps(a).Should().BeFalse();
    }

    [Fact]
    public void Overlaps_DisjointRanges_ReturnsFalse()
    {
        var a = DateRange.Of(D(10), D(12));
        var b = DateRange.Of(D(20), D(25));

        a.Overlaps(b).Should().BeFalse();
    }

    [Fact]
    public void Equality_SameDates_AreEqual()
    {
        DateRange.Of(D(10), D(15)).Should().Be(DateRange.Of(D(10), D(15)));
    }
}