using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class RoomAvailabilityConfiguration : IEntityTypeConfiguration<RoomAvailability>
{
    public void Configure(EntityTypeBuilder<RoomAvailability> builder)
    {
        builder.ToTable("RoomAvailabilities");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Status).HasConversion<string>().HasMaxLength(20);

        builder.OwnsOne(a => a.Range, range =>
        {
            range.Property(r => r.StartDate).HasColumnName("StartDate");
            range.Property(r => r.EndDate).HasColumnName("EndDate");
        });

        builder.HasOne(a => a.Room).WithMany(r => r.Availabilities).HasForeignKey(a => a.RoomId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Booking>().WithMany().HasForeignKey(a => a.BookingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(a => a.Range).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasIndex(a => new { a.RoomId, a.BookingId });
    }
}