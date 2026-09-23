using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class CityConfiguration : IEntityTypeConfiguration<City>
{
    public void Configure(EntityTypeBuilder<City> builder)
    {
        builder.ToTable("Cities");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name).HasMaxLength(150).IsRequired();
        builder.Property(c => c.Country).HasMaxLength(100).IsRequired();
        builder.Property(c => c.PostOffice).HasMaxLength(20).IsRequired();
        builder.Property(c => c.ThumbnailUrl).HasMaxLength(2048); // optional, same limit as image URLs
        builder.HasIndex(c => new { c.Name, c.Country }).IsUnique();

        builder.HasMany(c => c.Hotels).WithOne(h => h.City).HasForeignKey(h => h.CityId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(c => c.Hotels).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}