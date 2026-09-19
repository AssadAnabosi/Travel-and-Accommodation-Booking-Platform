using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Domain.ValueObjects;
using FluentAssertions;

namespace Domain.UnitTests.Entities;

public class RoomTests
{
    private static DateOnly D(int day) => new(2026, 1, day);
    private static DateRange Range(int start, int end) => DateRange.Of(D(start), D(end));
    private static Room NewRoom() => Room.Create(1, "101", RoomType.Standard, 2, 1, Money.Of(200m));

    [Fact]
    public void Create_SetsProperties_AndIsActive()
    {
        var room = NewRoom();

        room.HotelId.Should().Be(1);
        room.Number.Should().Be("101");
        room.RoomType.Should().Be(RoomType.Standard);
        room.AdultCapacity.Should().Be(2);
        room.ChildCapacity.Should().Be(1);
        room.BasePrice.Should().Be(Money.Of(200m));
        room.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_NonPositiveAdultCapacity_Throws()
    {
        var act = () => Room.Create(1, "101", RoomType.Standard, 0, 1, Money.Of(200m));

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Create_NegativeChildCapacity_Throws()
    {
        var act = () => Room.Create(1, "101", RoomType.Standard, 2, -1, Money.Of(200m));

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void IsAvailableFor_NewRoom_ReturnsTrue()
    {
        NewRoom().IsAvailableFor(Range(10, 15)).Should().BeTrue();
    }

    [Fact]
    public void IsAvailableFor_OverlappingBlock_ReturnsFalse()
    {
        var room = NewRoom();
        room.Block(Range(10, 15));

        room.IsAvailableFor(Range(12, 18)).Should().BeFalse();
    }

    [Fact]
    public void IsAvailableFor_NonOverlappingBlock_ReturnsTrue()
    {
        var room = NewRoom();
        room.Block(Range(10, 15));

        room.IsAvailableFor(Range(15, 20)).Should().BeTrue();
    }

    [Fact]
    public void IsAvailableFor_RetiredRoom_ReturnsFalse()
    {
        var room = NewRoom();
        room.Retire();

        room.IsAvailableFor(Range(10, 15)).Should().BeFalse();
    }

    [Fact]
    public void Reserve_MarksRangeBookedWithBookingId()
    {
        var room = NewRoom();
        var bookingId = Guid.NewGuid();

        var availability = room.Reserve(Range(10, 15), bookingId);

        availability.Status.Should().Be(AvailabilityStatus.Booked);
        availability.BookingId.Should().Be(bookingId);
        room.IsAvailableFor(Range(10, 15)).Should().BeFalse();
    }

    [Fact]
    public void Reserve_OverlappingRange_Throws()
    {
        var room = NewRoom();
        room.Reserve(Range(10, 15), Guid.NewGuid());

        var act = () => room.Reserve(Range(12, 18), Guid.NewGuid());

        act.Should().Throw<RoomNotAvailableException>();
    }

    [Fact]
    public void Block_MarksRangeBlockedWithoutBookingId()
    {
        var room = NewRoom();

        var availability = room.Block(Range(10, 15));

        availability.Status.Should().Be(AvailabilityStatus.Blocked);
        availability.BookingId.Should().BeNull();
    }

    [Fact]
    public void Block_OverlappingRange_Throws()
    {
        var room = NewRoom();
        room.Block(Range(10, 15));

        var act = () => room.Block(Range(14, 20));

        act.Should().Throw<RoomNotAvailableException>();
    }

    [Fact]
    public void Unblock_RemovesBlockedRange()
    {
        var room = NewRoom();
        var availability = room.Block(Range(10, 15));

        room.Unblock(availability);

        room.IsAvailableFor(Range(10, 15)).Should().BeTrue();
    }

    [Fact]
    public void Unblock_BookedRange_Throws()
    {
        var room = NewRoom();
        var availability = room.Reserve(Range(10, 15), Guid.NewGuid());

        var act = () => room.Unblock(availability);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void GetActivePrice_NoDiscounts_ReturnsBasePrice()
    {
        NewRoom().GetActivePrice(D(12)).Should().Be(Money.Of(200m));
    }

    [Fact]
    public void MarkDeleted_DeactivatesAndManglesNumber()
    {
        var room = NewRoom();

        room.MarkDeleted();

        room.IsActive.Should().BeFalse();
        room.Number.Should().StartWith("101::deleted::");
    }

    [Fact]
    public void MarkDeleted_WhenAlreadyInactive_Throws()
    {
        var room = NewRoom();
        room.MarkDeleted();

        var act = () => room.MarkDeleted();

        act.Should().Throw<InvalidOperationException>();
    }
}