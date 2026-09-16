using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class AmenityConfiguration : IEntityTypeConfiguration<Amenity>
{
    public void Configure(EntityTypeBuilder<Amenity> builder)
    {
        builder.ToTable("Amenities");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Name).HasMaxLength(100).IsRequired();
        builder.HasIndex(a => a.Name).IsUnique();

        builder.HasMany(a => a.HotelAmenities).WithOne(ha => ha.Amenity).HasForeignKey(ha => ha.AmenityId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(a => a.HotelAmenities).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}