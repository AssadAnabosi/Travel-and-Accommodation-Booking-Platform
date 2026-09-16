using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class HotelAmenityConfiguration : IEntityTypeConfiguration<HotelAmenity>
{
    public void Configure(EntityTypeBuilder<HotelAmenity> builder)
    {
        builder.ToTable("HotelAmenities");
        builder.HasKey(ha => new { ha.HotelId, ha.AmenityId });

        builder.HasOne(ha => ha.Hotel).WithMany(h => h.HotelAmenities).HasForeignKey(ha => ha.HotelId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(ha => ha.Amenity).WithMany(a => a.HotelAmenities).HasForeignKey(ha => ha.AmenityId)
            .OnDelete(DeleteBehavior.Restrict);
        // Restrict on Amenity — matches DeleteAmenityCommandHandler's explicit "blocked if in use" check.
    }
}