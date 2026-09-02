using Microsoft.EntityFrameworkCore;
using ZeugnisApp.Domain.Entities;
using ZeugnisApp.Domain.Enums;
using ZeugnisApp.Infrastructure.Persistence;

namespace ZeugnisApp.Infrastructure;

/// <summary>
/// Applies pending EF Core migrations on startup (plan §2 — the schema will evolve, so the app
/// must migrate forward on launch, never <c>EnsureCreated</c>) and seeds the one-time reference
/// data: the standard report forms, the school's phrase library and its identity.
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
        await SeedFloskelnAsync(db, cancellationToken).ConfigureAwait(false);
        await SeedSchoolSettingsAsync(db, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Seeds the standard templates once, into a database that has never held any
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

    /// <summary>
    /// Seeds Grundschule Wehringhausen's real phrase library once, into a database that has never
    /// held any phrase. Deleting phrases does not re-seed them.
    /// </summary>
    private static async Task SeedFloskelnAsync(ZeugnisDbContext db, CancellationToken cancellationToken)
    {
        var everExisted = await db.Floskeln.IgnoreQueryFilters()
            .AnyAsync(cancellationToken).ConfigureAwait(false);
        if (everExisted)
        {
            return;
        }

        db.Floskeln.AddRange(FloskelSeed.Standardfloskeln());
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Pre-fills the singleton settings row with the school's identity on first run so the teacher
    /// does not have to type it. Only runs when no settings row exists yet; never overwrites edits.
    /// </summary>
    private static async Task SeedSchoolSettingsAsync(ZeugnisDbContext db, CancellationToken cancellationToken)
    {
        var exists = await db.SchoolSettings
            .AnyAsync(s => s.Id == SchoolSettings.WellKnownId, cancellationToken)
            .ConfigureAwait(false);
        if (exists)
        {
            return;
        }

        db.SchoolSettings.Add(new SchoolSettings
        {
            Id = SchoolSettings.WellKnownId,
            Name = "Grundschule Wehringhausen",
            Zusatzbezeichnung = "Städtische Gemeinschaftsgrundschule",
            Adresse = "Minervastr. 37, 58089 Hagen",
            Schulnummer = "100189",
            Schulleitung = "J. Rohn",
            AktuellesHalbjahr = Halbjahr.Erstes,
        });
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
