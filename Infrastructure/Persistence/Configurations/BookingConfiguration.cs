using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.ToTable("Bookings");
        builder.HasKey(b => b.Id);

        builder.Property(b => b.ConfirmationNumber).HasMaxLength(50).IsRequired();
        builder.HasIndex(b => b.ConfirmationNumber).IsUnique();

        builder.Property(b => b.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(b => b.SpecialRequests).HasMaxLength(2000);

        builder.OwnsOne(b => b.StayRange, range =>
        {
            range.Property(r => r.StartDate).HasColumnName("CheckInDate");
            range.Property(r => r.EndDate).HasColumnName("CheckOutDate");
        });

        builder.OwnsOne(b => b.TotalPrice, price =>
        {
            price.Property(m => m.Amount).HasColumnName("TotalPriceAmount").HasColumnType("decimal(18,2)");
            price.Property(m => m.Currency).HasColumnName("TotalPriceCurrency").HasMaxLength(3);
        });

        // Restrict, not Cascade — a Room can only be hard-deleted when HasAnyBookingsAsync is false,
        // so this should never actually fire, but it's the correct safety net if that check is ever bypassed.
        builder.HasOne(b => b.Room).WithMany().HasForeignKey(b => b.RoomId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(b => b.User).WithMany(u => u.Bookings).HasForeignKey(b => b.UserId).OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(b => b.StayRange).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(b => b.TotalPrice).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}