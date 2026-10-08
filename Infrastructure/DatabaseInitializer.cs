using Infrastructure.Persistence;
using Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure;

public static class DatabaseInitializer
{
    /// <summary>
    /// Applies pending migrations and seeds sample data — but ONLY when isDevelopment is true.
    /// In any other environment this is a deliberate no-op; production databases are migrated
    /// and populated through an explicit, separate process, never automatically on app startup.
    /// </summary>
    public static async Task InitializeAsync(AppDbContext context, bool isDevelopment,
        CancellationToken cancellationToken = default)
    {
        if (!isDevelopment) return;

        await context.Database.MigrateAsync(cancellationToken);
        await DevSeeder.SeedAsync(context, cancellationToken);
    }
}