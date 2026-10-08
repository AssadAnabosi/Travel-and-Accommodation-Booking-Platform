using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class RoomImageConfiguration : IEntityTypeConfiguration<RoomImage>
{
    public void Configure(EntityTypeBuilder<RoomImage> builder)
    {
        builder.ToTable("RoomImages");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Url).HasMaxLength(2048).IsRequired();
        builder.HasIndex(i => new { i.RoomId, i.DisplayOrder });

        builder.HasOne(i => i.Room).WithMany(r => r.Images).HasForeignKey(i => i.RoomId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}