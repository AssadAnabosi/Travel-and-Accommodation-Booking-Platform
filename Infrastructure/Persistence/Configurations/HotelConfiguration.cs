using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class HotelConfiguration : IEntityTypeConfiguration<Hotel>
{
    public void Configure(EntityTypeBuilder<Hotel> builder)
    {
        builder.ToTable("Hotels");
        builder.HasKey(h => h.Id);

        builder.Property(h => h.Name).HasMaxLength(200).IsRequired();
        builder.Property(h => h.Description).HasMaxLength(4000);
        builder.Property(h => h.Address).HasMaxLength(300).IsRequired();
        builder.Property(h => h.Latitude).HasColumnType("float");
        builder.Property(h => h.Longitude).HasColumnType("float");
        builder.Property(h => h.ApprovalStatus).HasConversion<string>().HasMaxLength(20);
        builder.Property(h => h.RejectionReason).HasMaxLength(1000);

        // Restrict — matches DeleteCityCommandHandler's "blocked if hotels exist" check.
        builder.HasOne(h => h.City).WithMany(c => c.Hotels).HasForeignKey(h => h.CityId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(h => h.Owner).WithMany(u => u.OwnedHotels).HasForeignKey(h => h.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        // Cascade — these fully belong to the Hotel and DeleteHotelCommandHandler already
        // blocks deletion while Rooms exist, so this only fires for genuinely empty hotels.
        builder.HasMany(h => h.Rooms).WithOne(r => r.Hotel).HasForeignKey(r => r.HotelId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(h => h.Reviews).WithOne(r => r.Hotel).HasForeignKey(r => r.HotelId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(h => h.Images).WithOne(i => i.Hotel).HasForeignKey(i => i.HotelId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(h => h.Rooms).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(h => h.Reviews).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(h => h.HotelAmenities).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(h => h.Images).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}