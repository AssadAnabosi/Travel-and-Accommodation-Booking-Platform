using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Seed;

/// <summary>
/// AI GENERATED SEEDER ON PURPOSE!
/// DEV-ONLY sample data. Never referenced from any code path that also runs in Production —
/// DatabaseInitializer is the sole caller, and it's gated on isDevelopment before this is reached.
/// Seeds in phases (save parents, then attach children) because entities expose no navigation
/// adders and their factories/helpers need the parent's generated identity Id.
/// </summary>
public static class DevSeeder
{
    private const string SamplePassword = "Password123!";

    public static async Task SeedAsync(AppDbContext context, CancellationToken cancellationToken)
    {
        if (await context.Users.AnyAsync(cancellationToken)) return; // already seeded, don't duplicate on restart

        var hasher = new PasswordHasher();
        var passwordHash = hasher.Hash(SamplePassword);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // --- Phase 1: users ---
        var admin = User.Create("admin@tabp.dev", passwordHash, "Site", "Admin", UserRole.Admin);
        var owner = User.Create("owner@tabp.dev", passwordHash, "Olivia", "Owner", UserRole.HotelOwner);
        var customer = User.Create("customer@tabp.dev", passwordHash, "Charlie", "Customer", UserRole.Customer);
        context.Users.AddRange(admin, owner, customer);
        await context.SaveChangesAsync(cancellationToken);

        // --- Phase 2: cities + amenities ---
        var paris = City.Create("Paris", "France", "75001");
        var tokyo = City.Create("Tokyo", "Japan", "100-0001");
        var newYork = City.Create("New York", "United States", "10001");
        context.Cities.AddRange(paris, tokyo, newYork);

        var wifi = Amenity.Create("Free WiFi");
        var pool = Amenity.Create("Swimming Pool");
        var parking = Amenity.Create("Parking");
        var gym = Amenity.Create("Fitness Center");
        var spa = Amenity.Create("Spa");
        var restaurant = Amenity.Create("Restaurant");
        context.Amenities.AddRange(wifi, pool, parking, gym, spa, restaurant);

        await context.SaveChangesAsync(cancellationToken);

        // --- Phase 3a: hotels ---
        var grandParis = Hotel.CreateByAdmin("Le Grand Paris", 5,
            "An elegant five-star retreat steps from the Louvre.",
            "12 Rue de Rivoli", 48.8606, 2.3376, paris.Id, owner.Id);

        var tokyoBay = Hotel.CreateByAdmin("Tokyo Bay Tower", 4,
            "Modern comfort with skyline views over Tokyo Bay.",
            "3-1 Daiba, Minato City", 35.6270, 139.7730, tokyo.Id, owner.Id);

        var manhattanInn = Hotel.CreateByAdmin("Manhattan Central Inn", 3,
            "A cosy, well-located base in the heart of Midtown.",
            "255 W 43rd St", 40.7590, -73.9865, newYork.Id, owner.Id);

        // A pending hotel so the admin approval queue has something to show.
        var pendingHotel = Hotel.CreateByOwner("Riverside Boutique", 4,
            "Awaiting approval — a boutique stay by the Seine.",
            "5 Quai de Montebello", 48.8520, 2.3470, paris.Id, owner.Id);

        context.Hotels.AddRange(grandParis, tokyoBay, manhattanInn, pendingHotel);
        await context.SaveChangesAsync(cancellationToken);

        // --- Phase 3b: hotel amenities + images (need real hotel Ids) ---
        grandParis.SetAmenities([wifi.Id, pool.Id, spa.Id, restaurant.Id, gym.Id]);
        grandParis.AddImage("https://picsum.photos/seed/grandparis1/800/600");
        grandParis.AddImage("https://picsum.photos/seed/grandparis2/800/600");

        tokyoBay.SetAmenities([wifi.Id, gym.Id, restaurant.Id]);
        tokyoBay.AddImage("https://picsum.photos/seed/tokyobay1/800/600");

        manhattanInn.SetAmenities([wifi.Id, parking.Id]);
        manhattanInn.AddImage("https://picsum.photos/seed/manhattan1/800/600");

        await context.SaveChangesAsync(cancellationToken);

        // --- Phase 4a: rooms (need real hotel Ids) ---
        var parisDeluxe = Room.Create(grandParis.Id, "101", RoomType.Deluxe, 2, 1, Money.Of(320m));
        var parisSuite = Room.Create(grandParis.Id, "201", RoomType.Suite, 3, 2, Money.Of(540m));
        var tokyoStandard = Room.Create(tokyoBay.Id, "1201", RoomType.Standard, 2, 0, Money.Of(180m));
        var tokyoLuxury = Room.Create(tokyoBay.Id, "1801", RoomType.Luxury, 2, 2, Money.Of(410m));
        var nyBudget = Room.Create(manhattanInn.Id, "12", RoomType.Budget, 2, 0, Money.Of(140m));

        context.Rooms.AddRange(parisDeluxe, parisSuite, tokyoStandard, tokyoLuxury, nyBudget);
        await context.SaveChangesAsync(cancellationToken);

        // --- Phase 4b: room images (need real room Ids) ---
        parisDeluxe.AddImage("https://picsum.photos/seed/parisdeluxe/800/600");
        parisSuite.AddImage("https://picsum.photos/seed/parissuite/800/600");
        tokyoStandard.AddImage("https://picsum.photos/seed/tokyostd/800/600");
        tokyoLuxury.AddImage("https://picsum.photos/seed/tokyolux/800/600");
        nyBudget.AddImage("https://picsum.photos/seed/nybudget/800/600");
        await context.SaveChangesAsync(cancellationToken);

        // --- Phase 5: discounts, a blocked range, reviews, visits (need real room/hotel Ids) ---
        // Active-today discounts so these rooms surface in Featured Deals.
        context.Discounts.AddRange(
            Discount.Create(parisSuite.Id, "Autumn Escape", DiscountType.Percentage, 20m,
                today.AddDays(-2), today.AddMonths(2)),
            Discount.Create(tokyoLuxury.Id, "Bay View Special", DiscountType.FixedAmount, 60m,
                today.AddDays(-2), today.AddMonths(1)));

        // A manually blocked range so availability filtering has something to exclude.
        tokyoStandard.Block(DateRange.Of(today.AddDays(3), today.AddDays(6)));

        context.Reviews.AddRange(
            Review.Create(grandParis.Id, customer.Id, 5, "Impeccable service and a stunning location."),
            Review.Create(tokyoBay.Id, customer.Id, 4, "Great views, would happily return."),
            Review.Create(manhattanInn.Id, customer.Id, 3, "Compact but perfectly located."));

        // Visits drive Trending Destinations (Paris gets the most).
        context.HotelVisits.AddRange(
            HotelVisit.Record(customer.Id, grandParis.Id),
            HotelVisit.Record(customer.Id, grandParis.Id),
            HotelVisit.Record(null, grandParis.Id),
            HotelVisit.Record(customer.Id, tokyoBay.Id),
            HotelVisit.Record(null, manhattanInn.Id));

        await context.SaveChangesAsync(cancellationToken);
    }
}
