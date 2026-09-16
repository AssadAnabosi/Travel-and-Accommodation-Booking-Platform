using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class HotelVisitConfiguration : IEntityTypeConfiguration<HotelVisit>
{
    public void Configure(EntityTypeBuilder<HotelVisit> builder)
    {
        builder.ToTable("HotelVisits");
        builder.HasKey(v => v.Id);

        builder.HasOne(v => v.User).WithMany().HasForeignKey(v => v.UserId)
            .OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(v => v.Hotel).WithMany().HasForeignKey(v => v.HotelId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(v => new { v.UserId, v.VisitedAt });
        builder.HasIndex(v => new { v.HotelId, v.VisitedAt });
    }
}