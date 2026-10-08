using System.Reflection;
using System.Text;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using FluentAssertions;
using Infrastructure.Services;
using PdfSharpCore.Fonts;
using PdfSharpCore.Pdf.IO;

namespace Infrastructure.UnitTests.Services;

public class PdfGeneratorTests
{
    static PdfGeneratorTests()
    {
        // Mirrors Infrastructure.DependencyInjection: the resolver is process-global and set once.
        GlobalFontSettings.FontResolver ??= new FileFontResolver("AppSans");
    }

    private readonly PdfGenerator _generator = new();

    private static Booking ABooking(string? specialRequests = null)
    {
        var user = User.Create("guest@tabp.dev", "hash", "Jane", "Doe");
        var hotel = Hotel.CreateByAdmin("Grand Plaza", 4, "Nice place", "1 Main St", 31.9, 35.2, 1, Guid.NewGuid());
        var room = Room.Create(hotel.Id, "101", RoomType.Deluxe, 2, 1, Money.Of(120m));
        var booking = Booking.Create(user.Id, room.Id,
            DateRange.Of(new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 4)), 2, 1, Money.Of(360m),
            specialRequests);

        // Navigations have private setters (EF populates them); set them the same way for the test.
        SetNavigation(room, nameof(Room.Hotel), hotel);
        SetNavigation(booking, nameof(Booking.User), user);
        SetNavigation(booking, nameof(Booking.Room), room);
        return booking;
    }

    private static void SetNavigation(object entity, string property, object value) =>
        entity.GetType().GetProperty(property, BindingFlags.Instance | BindingFlags.Public)!
            .SetValue(entity, value);

    [Fact]
    public void GenerateBookingConfirmation_ReturnsWellFormedPdf()
    {
        var bytes = _generator.GenerateBookingConfirmation(ABooking());

        Encoding.ASCII.GetString(bytes, 0, 5).Should().Be("%PDF-");
        Encoding.ASCII.GetString(bytes[^32..]).Should().Contain("%%EOF");
    }

    [Fact]
    public void GenerateBookingConfirmation_ProducesSingleA4PageThatReopens()
    {
        var bytes = _generator.GenerateBookingConfirmation(ABooking("Late check-in please"));

        using var document = PdfReader.Open(new MemoryStream(bytes), PdfDocumentOpenMode.Import);
        document.PageCount.Should().Be(1);
        document.Pages[0].Width.Point.Should().BeApproximately(595, 1);
        document.Pages[0].Height.Point.Should().BeApproximately(842, 1);
    }
}
