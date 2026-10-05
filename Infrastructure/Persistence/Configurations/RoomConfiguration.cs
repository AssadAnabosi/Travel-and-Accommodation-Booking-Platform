using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class RoomConfiguration : IEntityTypeConfiguration<Room>
{
    public void Configure(EntityTypeBuilder<Room> builder)
    {
        builder.ToTable("Rooms");
        builder.HasKey(r => r.Id);

        // Widened to accommodate the "::deleted::<guid>" suffix Room.MarkDeleted() appends
        builder.Property(r => r.Number).HasMaxLength(100).IsRequired();

        builder.Property(r => r.RoomType).HasConversion<string>().HasMaxLength(30);
        builder.Property(r => r.AdultCapacity).IsRequired();
        builder.Property(r => r.ChildCapacity).IsRequired();
        builder.Property(r => r.IsActive).IsRequired();

        // Optimistic concurrency guard against double-booking
        builder.Property(r => r.RowVersion).IsRowVersion();

        builder.OwnsOne(r => r.BasePrice, price =>
        {
            price.Property(m => m.Amount).HasColumnName("BasePriceAmount").HasColumnType("decimal(18,2)");
            price.Property(m => m.Currency).HasColumnName("BasePriceCurrency").HasMaxLength(3);
        });

        // Enforced at the DB level too — app-layer uniqueness check in CreateRoomCommandValidator
        // is defense-in-depth's other half. Mangled deleted-room numbers never collide with this.
        builder.HasIndex(r => new { r.HotelId, r.Number }).IsUnique();

        builder.HasMany(r => r.Availabilities).WithOne(a => a.Room).HasForeignKey(a => a.RoomId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(r => r.Discounts).WithOne(d => d.Room).HasForeignKey(d => d.RoomId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(r => r.Images).WithOne(i => i.Room).HasForeignKey(i => i.RoomId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(r => r.Availabilities).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(r => r.Discounts).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(r => r.Images).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}