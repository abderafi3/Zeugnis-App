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

        await SeedStandardFormulareAsync(db, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Seeds the seven standard templates once, into a database that has never held any
    /// (plan §Sprint 5). Uses <see cref="EntityFrameworkQueryableExtensions.IgnoreQueryFilters"/>
    /// so a teacher who deletes them does not get them back.
    /// </summary>
    private static async Task SeedStandardFormulareAsync(ZeugnisDbContext db, CancellationToken cancellationToken)
    {
        var everExisted = await db.Formulare.IgnoreQueryFilters()
            .AnyAsync(cancellationToken).ConfigureAwait(false);
        if (everExisted)
        {
            return;
        }

        db.Formulare.AddRange(FormularSeed.Standardformulare());
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
