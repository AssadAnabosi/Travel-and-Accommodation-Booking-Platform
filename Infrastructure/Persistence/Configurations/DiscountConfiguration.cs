using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class DiscountConfiguration : IEntityTypeConfiguration<Discount>
{
    public void Configure(EntityTypeBuilder<Discount> builder)
    {
        builder.ToTable("Discounts");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Name).HasMaxLength(200).IsRequired();
        builder.Property(d => d.Type).HasConversion<string>().HasMaxLength(20);
        builder.Property(d => d.Value).HasColumnType("decimal(18,2)");

        builder.HasOne(d => d.Room).WithMany(r => r.Discounts).HasForeignKey(d => d.RoomId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(d => new { d.RoomId, d.StartDate, d.EndDate });
    }
}