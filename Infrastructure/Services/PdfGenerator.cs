using Application.Common.Interfaces.Services;
using Domain.Entities;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;

namespace Infrastructure.Services;

/// <summary>
/// Renders a booking confirmation as a single-page PDF. Assumes the passed booking has its
/// User, Room and Room.Hotel navigations loaded.
/// </summary>
public class PdfGenerator : IPdfGenerator
{
    public byte[] GenerateBookingConfirmation(Booking booking)
    {
        using var document = new PdfDocument();
        var page = document.AddPage();
        page.Size = PdfSharpCore.PageSize.A4;

        using var gfx = XGraphics.FromPdfPage(page);
        var titleFont = new XFont("Arial", 20, XFontStyle.Bold);
        var labelFont = new XFont("Arial", 11, XFontStyle.Bold);
        var valueFont = new XFont("Arial", 11, XFontStyle.Regular);

        const double left = 50;
        const double labelWidth = 140;
        var y = 60.0;

        gfx.DrawString("Booking Confirmation", titleFont, XBrushes.Black, new XPoint(left, y));
        y += 40;

        void Row(string label, string value)
        {
            gfx.DrawString(label, labelFont, XBrushes.Black, new XPoint(left, y));
            gfx.DrawString(value, valueFont, XBrushes.Black, new XPoint(left + labelWidth, y));
            y += 24;
        }

        Row("Confirmation #", booking.ConfirmationNumber);
        Row("Status", booking.Status.ToString());
        Row("Guest", $"{booking.User.FirstName} {booking.User.LastName}");
        Row("Hotel", booking.Room.Hotel.Name);
        Row("Room", $"{booking.Room.Number} ({booking.Room.RoomType})");
        Row("Check-in", booking.StayRange.StartDate.ToString("yyyy-MM-dd"));
        Row("Check-out", booking.StayRange.EndDate.ToString("yyyy-MM-dd"));
        Row("Guests", $"{booking.Adults} adult(s), {booking.Children} child(ren)");
        Row("Total", $"{booking.TotalPrice.Amount:0.00} {booking.TotalPrice.Currency}");

        if (!string.IsNullOrWhiteSpace(booking.SpecialRequests))
            Row("Special requests", booking.SpecialRequests);

        using var stream = new MemoryStream();
        document.Save(stream);
        return stream.ToArray();
    }
}