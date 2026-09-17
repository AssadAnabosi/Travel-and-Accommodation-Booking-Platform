using Infrastructure.Persistence;

namespace Infrastructure.Persistence.Seed;

/// <summary>
/// DEV-ONLY sample data. Never referenced from any code path that also runs in Production —
/// DatabaseInitializer is the sole caller, and it's gated on isDevelopment before this is reached.
/// </summary>
public static class DevSeeder
{
    public static async Task SeedAsync(AppDbContext context, CancellationToken cancellationToken)
    {
        if (context.Users.Any()) return; // already seeded, don't duplicate on every restart

        // TODO next pass: seed one Admin user, a couple of Cities, a couple of Approved Hotels
        // with Rooms/Images/Amenities, once IPasswordHasher exists to hash the admin's password.
        await context.SaveChangesAsync(cancellationToken);
    }
}