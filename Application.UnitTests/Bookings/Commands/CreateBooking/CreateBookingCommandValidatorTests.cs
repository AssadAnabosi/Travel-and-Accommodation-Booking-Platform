using Application.Features.Bookings.Commands.CreateBooking;
using FluentValidation.TestHelper;

namespace Application.UnitTests.Bookings.Commands.CreateBooking;

public class CreateBookingCommandValidatorTests
{
    private readonly CreateBookingCommandValidator _validator = new();

    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow.Date);

    private static CreateBookingCommand Valid() =>
        new(RoomId: 1, CheckIn: Today.AddDays(1), CheckOut: Today.AddDays(3), Adults: 2, Children: 0, SpecialRequests: null);

    [Fact]
    public void ValidCommand_HasNoErrors()
    {
        _validator.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void NonPositiveRoomId_HasError()
    {
        _validator.TestValidate(Valid() with { RoomId = 0 })
            .ShouldHaveValidationErrorFor(x => x.RoomId);
    }

    [Fact]
    public void CheckInInPast_HasError()
    {
        _validator.TestValidate(Valid() with { CheckIn = Today.AddDays(-1) })
            .ShouldHaveValidationErrorFor(x => x.CheckIn);
    }

    [Fact]
    public void CheckOutNotAfterCheckIn_HasError()
    {
        var checkIn = Today.AddDays(2);
        _validator.TestValidate(Valid() with { CheckIn = checkIn, CheckOut = checkIn })
            .ShouldHaveValidationErrorFor(x => x.CheckOut);
    }

    [Fact]
    public void NonPositiveAdults_HasError()
    {
        _validator.TestValidate(Valid() with { Adults = 0 })
            .ShouldHaveValidationErrorFor(x => x.Adults);
    }

    [Fact]
    public void NegativeChildren_HasError()
    {
        _validator.TestValidate(Valid() with { Children = -1 })
            .ShouldHaveValidationErrorFor(x => x.Children);
    }
}