using Application.Common.Exceptions;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.Features.Bookings.Queries.GetBookingConfirmationPdf;
using Application.UnitTests.TestSupport;
using Domain.Entities;
using FluentAssertions;
using Moq;

namespace Application.UnitTests.Bookings.Queries.GetBookingConfirmationPdf;

public class GetBookingConfirmationPdfQueryHandlerTests
{
    private static readonly byte[] PdfBytes = "%PDF-1.4"u8.ToArray();

    private readonly Mock<IBookingRepository> _bookings = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Mock<IPdfGenerator> _pdf = new();
    private readonly Guid _hotelOwnerId = Guid.NewGuid();
    private readonly User _guest = User.Create("guest@tabp.dev", "hash", "Gina", "Guest");

    public GetBookingConfirmationPdfQueryHandlerTests() =>
        _pdf.Setup(p => p.GenerateBookingConfirmation(It.IsAny<Booking>())).Returns(PdfBytes);

    private GetBookingConfirmationPdfQueryHandler CreateHandler() =>
        new(_bookings.Object, _currentUser.Object, _pdf.Object);

    private Booking GivenBooking(bool confirmed)
    {
        var booking = TestData.BookingFor(_guest, TestData.RoomIn(TestData.Hotel(_hotelOwnerId)), confirmed);
        _bookings.Setup(b => b.GetByIdWithDetailsAsync(booking.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);
        return booking;
    }

    [Fact]
    public async Task Handle_ConfirmedBookingForGuest_ReturnsGeneratedPdf()
    {
        var booking = GivenBooking(confirmed: true);
        _currentUser.Setup(c => c.UserId).Returns(_guest.Id);

        var bytes = await CreateHandler().Handle(new GetBookingConfirmationPdfQuery(booking.Id), CancellationToken.None);

        bytes.Should().Equal(PdfBytes);
        _pdf.Verify(p => p.GenerateBookingConfirmation(booking), Times.Once);
    }

    [Fact]
    public async Task Handle_ConfirmedBookingForHotelOwner_IsAllowed()
    {
        var booking = GivenBooking(confirmed: true);
        _currentUser.Setup(c => c.UserId).Returns(_hotelOwnerId);

        var bytes = await CreateHandler().Handle(new GetBookingConfirmationPdfQuery(booking.Id), CancellationToken.None);

        bytes.Should().Equal(PdfBytes);
    }

    [Fact]
    public async Task Handle_PendingBooking_ThrowsConflictWithoutGenerating()
    {
        var booking = GivenBooking(confirmed: false);
        _currentUser.Setup(c => c.UserId).Returns(_guest.Id);

        var act = () => CreateHandler().Handle(new GetBookingConfirmationPdfQuery(booking.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        _pdf.Verify(p => p.GenerateBookingConfirmation(It.IsAny<Booking>()), Times.Never);
    }

    [Fact]
    public async Task Handle_UnrelatedUser_ThrowsForbiddenEvenWhenPending()
    {
        // The access check runs before the status check, so a stranger can't learn the booking's status.
        var booking = GivenBooking(confirmed: false);
        _currentUser.Setup(c => c.UserId).Returns(Guid.NewGuid());

        var act = () => CreateHandler().Handle(new GetBookingConfirmationPdfQuery(booking.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task Handle_BookingNotFound_ThrowsNotFound()
    {
        var act = () => CreateHandler().Handle(new GetBookingConfirmationPdfQuery(Guid.NewGuid()),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
