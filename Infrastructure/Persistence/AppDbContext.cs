using Application.Common.Exceptions;
using Application.Common.Interfaces.Persistence;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IUnitOfWork
{
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<City> Cities => Set<City>();
    public DbSet<Hotel> Hotels => Set<Hotel>();
    public DbSet<HotelImage> HotelImages => Set<HotelImage>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<RoomImage> RoomImages => Set<RoomImage>();
    public DbSet<RoomAvailability> RoomAvailabilities => Set<RoomAvailability>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<Amenity> Amenities => Set<Amenity>();
    public DbSet<HotelAmenity> HotelAmenities => Set<HotelAmenity>();
    public DbSet<Discount> Discounts => Set<Discount>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<HotelVisit> HotelVisits => Set<HotelVisit>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException(
                "The resource was modified by another request. Please retry.");
        }
    }
}