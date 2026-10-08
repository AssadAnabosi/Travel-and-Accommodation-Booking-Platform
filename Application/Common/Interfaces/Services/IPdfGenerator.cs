using Domain.Entities;

namespace Application.Common.Interfaces.Services;

public interface IPdfGenerator
{
    byte[] GenerateBookingConfirmation(Booking booking);
}