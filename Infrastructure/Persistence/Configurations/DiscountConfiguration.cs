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

        builder.Property(d => d.Name).HasMaxLength(150).IsRequired();
        builder.Property(d => d.Type).HasConversion<string>().HasMaxLength(20);
        builder.Property(d => d.Value).HasColumnType("decimal(18,2)");
        builder.Property(d => d.IsActive).IsRequired();

        builder.HasIndex(d => new { d.RoomId, d.StartDate, d.EndDate });
    }
}