using Microsoft.EntityFrameworkCore;
using ZeugnisApp.Infrastructure.Persistence;

namespace ZeugnisApp.Infrastructure;

/// <summary>
/// Applies pending EF Core migrations on startup (plan §2 — the schema will evolve, so the app
/// must migrate forward on launch, never <c>EnsureCreated</c>).
/// </summary>
public sealed class DatabaseInitializer(IDbContextFactory<ZeugnisDbContext> contextFactory)
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        await db.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);

        // WAL for crash-safety (plan §8). Persists in the file header; safe to re-assert on every start.
        await db.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;", cancellationToken).ConfigureAwait(false);
    }
}
