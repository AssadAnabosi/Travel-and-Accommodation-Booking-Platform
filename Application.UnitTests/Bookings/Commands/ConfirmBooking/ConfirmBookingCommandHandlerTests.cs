using Application.Common.Exceptions;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.Common.Models;
using Application.Features.Bookings.Commands.ConfirmBooking;
using Application.UnitTests.TestSupport;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Moq;

namespace Application.UnitTests.Bookings.Commands.ConfirmBooking;

public class ConfirmBookingCommandHandlerTests
{
    private static readonly byte[] PdfBytes = "%PDF-1.4"u8.ToArray();

    private readonly Mock<IBookingRepository> _bookings = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Mock<IPaymentGateway> _payments = new();
    private readonly Mock<IEmailService> _email = new();
    private readonly Mock<IPdfGenerator> _pdf = new();
    private readonly User _guest = User.Create("gina@tabp.dev", "hash", "Gina", "Guest");

    public ConfirmBookingCommandHandlerTests()
    {
        _currentUser.Setup(c => c.UserId).Returns(_guest.Id);
        _pdf.Setup(p => p.GenerateBookingConfirmation(It.IsAny<Booking>())).Returns(PdfBytes);
        _payments.Setup(p => p.ChargeAsync(It.IsAny<PaymentRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentResult(true, "MOCK-1", null));
    }

    private ConfirmBookingCommandHandler CreateHandler() =>
        new(_bookings.Object, _uow.Object, _currentUser.Object, _payments.Object, _email.Object, _pdf.Object);

    private Booking GivenBooking(bool confirmed = false, string hotelName = "Grand")
    {
        var booking = TestData.BookingFor(_guest,
            TestData.RoomIn(TestData.Hotel(Guid.NewGuid(), name: hotelName)), confirmed);
        _bookings.Setup(b => b.GetByIdWithDetailsAsync(booking.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);
        return booking;
    }

    [Fact]
    public async Task Handle_PaymentSucceeds_ChargesTotalConfirmsSavesAndReturnsDto()
    {
        var booking = GivenBooking();

        var dto = await CreateHandler().Handle(new ConfirmBookingCommand(booking.Id, "tok_visa"),
            CancellationToken.None);

        _payments.Verify(p => p.ChargeAsync(
            new PaymentRequest(booking.Id, 300m, "USD", "tok_visa"), It.IsAny<CancellationToken>()), Times.Once);
        booking.Status.Should().Be(BookingStatus.Confirmed);
        _bookings.Verify(b => b.Update(booking), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        dto.Id.Should().Be(booking.Id);
        dto.Status.Should().Be("Confirmed");
        dto.TotalPrice.Should().Be(300m);
        dto.CheckIn.Should().Be(new DateOnly(2026, 10, 1));
        dto.CheckOut.Should().Be(new DateOnly(2026, 10, 4));
    }

    [Fact]
    public async Task Handle_PaymentSucceeds_EmailsGuestWithPdfAttachedAndEncodedHotelName()
    {
        var booking = GivenBooking(hotelName: "<b>Grand</b>");

        await CreateHandler().Handle(new ConfirmBookingCommand(booking.Id, "tok_visa"), CancellationToken.None);

        _email.Verify(e => e.SendAsync(It.Is<EmailMessage>(m =>
                m.ToAddress == "gina@tabp.dev"
                && m.Attachment == PdfBytes
                && m.AttachmentFileName == "booking-confirmation.pdf"
                && m.HtmlBody.Contains(booking.ConfirmationNumber)
                && m.HtmlBody.Contains("&lt;b&gt;Grand&lt;/b&gt;")
                && !m.HtmlBody.Contains("<b>Grand")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_EmailIsSentOnlyAfterTheConfirmationIsSaved()
    {
        // Decision #69: the booking is paid + persisted first; email is best-effort afterwards.
        var booking = GivenBooking();
        var calls = new List<string>();
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).Callback(() => calls.Add("save"))
            .ReturnsAsync(1);
        _email.Setup(e => e.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .Callback(() => calls.Add("email")).Returns(Task.CompletedTask);

        await CreateHandler().Handle(new ConfirmBookingCommand(booking.Id, "tok_visa"), CancellationToken.None);

        calls.Should().Equal("save", "email");
    }

    [Fact]
    public async Task Handle_PaymentDeclined_ThrowsPaymentFailedAndLeavesBookingPending()
    {
        var booking = GivenBooking();
        _payments.Setup(p => p.ChargeAsync(It.IsAny<PaymentRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentResult(false, null, "Card declined"));

        var act = () => CreateHandler().Handle(new ConfirmBookingCommand(booking.Id, "tok_bad"),
            CancellationToken.None);

        (await act.Should().ThrowAsync<PaymentFailedException>()).WithMessage("*Card declined*");
        booking.Status.Should().Be(BookingStatus.Pending); // retryable
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _email.Verify(e => e.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_AlreadyConfirmed_ThrowsConflictWithoutChargingAgain()
    {
        var booking = GivenBooking(confirmed: true);

        var act = () => CreateHandler().Handle(new ConfirmBookingCommand(booking.Id, "tok_visa"),
            CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        _payments.Verify(p => p.ChargeAsync(It.IsAny<PaymentRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_SomeoneElsesBooking_ThrowsForbiddenWithoutCharging()
    {
        var booking = GivenBooking();
        _currentUser.Setup(c => c.UserId).Returns(Guid.NewGuid());

        var act = () => CreateHandler().Handle(new ConfirmBookingCommand(booking.Id, "tok_visa"),
            CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
        _payments.Verify(p => p.ChargeAsync(It.IsAny<PaymentRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_BookingNotFound_ThrowsNotFound()
    {
        var act = () => CreateHandler().Handle(new ConfirmBookingCommand(Guid.NewGuid(), "tok_visa"),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
