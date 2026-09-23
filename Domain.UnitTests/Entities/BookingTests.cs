using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Domain.ValueObjects;
using FluentAssertions;

namespace Domain.UnitTests.Entities;

public class BookingTests
{
    private static DateRange Stay() => DateRange.Of(new DateOnly(2026, 1, 10), new DateOnly(2026, 1, 15));

    private static Booking NewBooking(int adults = 2, int children = 0) =>
        Booking.Create(Guid.NewGuid(), roomId: 1, Stay(), adults, children, Money.Of(500m));

    [Fact]
    public void Create_StartsPending_WithConfirmationNumber()
    {
        var booking = NewBooking();

        booking.Status.Should().Be(BookingStatus.Pending);
        booking.ConfirmationNumber.Should().StartWith("HB-");
        booking.Adults.Should().Be(2);
    }

    [Fact]
    public void Create_NonPositiveAdults_Throws()
    {
        var act = () => NewBooking(adults: 0);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Create_NegativeChildren_Throws()
    {
        var act = () => NewBooking(children: -1);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void FullLifecycle_ConfirmCheckInCheckOut_EndsCheckedOut()
    {
        var booking = NewBooking();

        booking.Confirm();
        booking.Status.Should().Be(BookingStatus.Confirmed);

        booking.CheckIn();
        booking.Status.Should().Be(BookingStatus.CheckedIn);

        booking.CheckOut();
        booking.Status.Should().Be(BookingStatus.CheckedOut);
    }

    [Fact]
    public void Confirm_WhenNotPending_Throws()
    {
        var booking = NewBooking();
        booking.Confirm();

        var act = booking.Confirm;

        act.Should().Throw<InvalidStateTransitionException>();
    }

    [Fact]
    public void CheckIn_WhenNotConfirmed_Throws()
    {
        var act = () => NewBooking().CheckIn();

        act.Should().Throw<InvalidStateTransitionException>();
    }

    [Fact]
    public void CheckOut_WhenNotCheckedIn_Throws()
    {
        var booking = NewBooking();
        booking.Confirm();

        var act = booking.CheckOut;

        act.Should().Throw<InvalidStateTransitionException>();
    }

    [Fact]
    public void Cancel_PendingBooking_BecomesCancelled()
    {
        var booking = NewBooking();

        booking.Cancel();

        booking.Status.Should().Be(BookingStatus.Cancelled);
    }

    [Fact]
    public void Cancel_CheckedOutBooking_Throws()
    {
        var booking = NewBooking();
        booking.Confirm();
        booking.CheckIn();
        booking.CheckOut();

        var act = booking.Cancel;

        act.Should().Throw<InvalidStateTransitionException>();
    }
}