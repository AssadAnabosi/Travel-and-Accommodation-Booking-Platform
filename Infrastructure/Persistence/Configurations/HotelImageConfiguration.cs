using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class HotelImageConfiguration : IEntityTypeConfiguration<HotelImage>
{
    public void Configure(EntityTypeBuilder<HotelImage> builder)
    {
        builder.ToTable("HotelImages");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Url).HasMaxLength(2048).IsRequired();
        builder.HasIndex(i => new { i.HotelId, i.DisplayOrder });

        builder.HasOne(i => i.Hotel).WithMany(h => h.Images).HasForeignKey(i => i.HotelId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}