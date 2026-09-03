using ZeugnisApp.Application.Import;
using ZeugnisApp.Domain.Entities;
using ZeugnisApp.Domain.Enums;
using ZeugnisApp.Infrastructure.Import;
using ZeugnisApp.Infrastructure.Services;

namespace ZeugnisApp.Infrastructure.Tests;

public class FehlzeitenImportServiceTests
{
    private static async Task<(SqliteTestDatabase db, Guid klasseId)> SeedClassAsync()
    {
        var db = new SqliteTestDatabase();
        await db.MigrateAsync();
        await using var ctx = db.NewContext();
        var klasse = new Klasse { Name = "E1", Jahrgang = 1 };
        ctx.Klassen.Add(klasse);
        ctx.Kinder.AddRange(
            new Kind { Vorname = "Anna", Nachname = "Beispiel", Klasse = klasse },
            new Kind { Vorname = "Bruno Carlo", Nachname = "de Vries", Klasse = klasse },
            new Kind { Vorname = "Clara", Nachname = "Xu", Klasse = klasse });
        await ctx.SaveChangesAsync();
        return (db, klasse.Id);
    }

    [Fact]
    public async Task Matches_names_in_either_order_and_upserts_only_the_total()
    {
        var (db, klasseId) = await SeedClassAsync();
        await using var _ = db;
        var svc = new FehlzeitenImportService(db.ContextFactory);
        var fehlzeiten = new FehlzeitenService(db.ContextFactory);

        // Clara already has a manually-entered unexcused count that the import must not clobber.
        await fehlzeiten.SaveAsync((await FirstKindId(db, "Clara")), "2026/27", Halbjahr.Erstes, 10, 4);

        var rows = new[]
        {
            new FehlzeitImportData("Anna Beispiel", 8, null),
            new FehlzeitImportData("de Vries, Bruno Carlo", 12, null),
            new FehlzeitImportData("Clara Xu", 20, null),
        };

        var result = await svc.CommitAsync(klasseId, "2026/27", Halbjahr.Erstes, rows, 0);

        Assert.Equal(3, result.Importiert);
        Assert.Empty(result.NichtZugeordnet);

        var anna = await fehlzeiten.GetAsync(await FirstKindId(db, "Anna"), "2026/27", Halbjahr.Erstes);
        Assert.Equal(8, anna!.StundenGesamt);
        Assert.Equal(0, anna.StundenUnentschuldigt);

        var bruno = await fehlzeiten.GetAsync(await FirstKindId(db, "Bruno Carlo"), "2026/27", Halbjahr.Erstes);
        Assert.Equal(12, bruno!.StundenGesamt);

        // total grew, the earlier unexcused entry is kept (still within the new total).
        var clara = await fehlzeiten.GetAsync(await FirstKindId(db, "Clara"), "2026/27", Halbjahr.Erstes);
        Assert.Equal(20, clara!.StundenGesamt);
        Assert.Equal(4, clara.StundenUnentschuldigt);
    }

    [Fact]
    public async Task Unknown_names_are_reported_not_created()
    {
        var (db, klasseId) = await SeedClassAsync();
        await using var _ = db;
        var svc = new FehlzeitenImportService(db.ContextFactory);

        var rows = new[]
        {
            new FehlzeitImportData("Anna Beispiel", 3, null),
            new FehlzeitImportData("Fremd Kind", 9, null),
        };

        var result = await svc.CommitAsync(klasseId, "2026/27", Halbjahr.Erstes, rows, 0);

        Assert.Equal(1, result.Importiert);
        Assert.Equal("Fremd Kind", Assert.Single(result.NichtZugeordnet));
    }

    [Fact]
    public async Task An_explicit_unexcused_column_is_clamped_to_the_total()
    {
        var (db, klasseId) = await SeedClassAsync();
        await using var _ = db;
        var svc = new FehlzeitenImportService(db.ContextFactory);
        var fehlzeiten = new FehlzeitenService(db.ContextFactory);

        await svc.CommitAsync(klasseId, "2026/27", Halbjahr.Erstes,
            [new FehlzeitImportData("Anna Beispiel", 6, 99)], 0);

        var anna = await fehlzeiten.GetAsync(await FirstKindId(db, "Anna"), "2026/27", Halbjahr.Erstes);
        Assert.Equal(6, anna!.StundenGesamt);
        Assert.Equal(6, anna.StundenUnentschuldigt);
    }

    private static async Task<Guid> FirstKindId(SqliteTestDatabase db, string vorname)
    {
        await using var ctx = db.NewContext();
        return ctx.Kinder.First(k => k.Vorname == vorname).Id;
    }
}
