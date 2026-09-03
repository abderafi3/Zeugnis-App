using Microsoft.EntityFrameworkCore;
using ZeugnisApp.Application;
using ZeugnisApp.Application.Formulare;
using ZeugnisApp.Domain.Entities;
using ZeugnisApp.Domain.Enums;
using ZeugnisApp.Infrastructure.Services;

namespace ZeugnisApp.Infrastructure.Tests;

public class FehlzeitenServiceTests
{
    private static async Task<(SqliteTestDatabase db, Guid klasseId, Guid[] kinder)> SeedClassAsync()
    {
        var db = new SqliteTestDatabase();
        await db.MigrateAsync();

        await using var ctx = db.NewContext();
        var klasse = new Klasse { Name = "3a", Jahrgang = 3 };
        var a = new Kind { Vorname = "Anna", Nachname = "A", Klasse = klasse };
        var b = new Kind { Vorname = "Ben", Nachname = "B", Klasse = klasse };
        ctx.AddRange(klasse, a, b);
        await ctx.SaveChangesAsync();
        return (db, klasse.Id, [a.Id, b.Id]);
    }

    [Fact]
    public async Task GetForKlasse_returns_a_row_per_child_with_zeros_when_nothing_entered()
    {
        var (db, klasseId, _) = await SeedClassAsync();
        await using var _ = db;
        var svc = new FehlzeitenService(db.ContextFactory);

        var rows = await svc.GetForKlasseAsync(klasseId, "2026/27", Halbjahr.Erstes);

        Assert.Equal(2, rows.Count);
        Assert.All(rows, r => Assert.Equal(0, r.StundenGesamt));
    }

    [Fact]
    public async Task SaveAsync_upserts_and_GetForKlasse_reflects_it()
    {
        var (db, klasseId, kinder) = await SeedClassAsync();
        await using var _ = db;
        var svc = new FehlzeitenService(db.ContextFactory);

        await svc.SaveAsync(kinder[0], "2026/27", Halbjahr.Erstes, stundenGesamt: 12, stundenUnentschuldigt: 4);
        await svc.SaveAsync(kinder[0], "2026/27", Halbjahr.Erstes, stundenGesamt: 10, stundenUnentschuldigt: 2); // update

        var row = (await svc.GetForKlasseAsync(klasseId, "2026/27", Halbjahr.Erstes)).First(r => r.KindId == kinder[0]);
        Assert.Equal(10, row.StundenGesamt);
        Assert.Equal(2, row.StundenUnentschuldigt);
    }

    [Fact]
    public async Task SaveAsync_rejects_unexcused_greater_than_total()
    {
        var (db, _, kinder) = await SeedClassAsync();
        await using var _ = db;
        var svc = new FehlzeitenService(db.ContextFactory);

        await Assert.ThrowsAsync<EntityValidationException>(
            () => svc.SaveAsync(kinder[0], "2026/27", Halbjahr.Erstes, stundenGesamt: 3, stundenUnentschuldigt: 5));
    }

    [Fact]
    public async Task A_report_mirrors_the_class_table_until_its_absences_are_overridden()
    {
        var (db, _, kinder) = await SeedClassAsync();
        await using var _ = db;

        await using (var ctx = db.NewContext())
        {
            ctx.Formulare.AddRange(StandardFormulare.All());
            await ctx.SaveChangesAsync();
        }

        var fehlzeiten = new FehlzeitenService(db.ContextFactory);
        await fehlzeiten.SaveAsync(kinder[0], "2026/27", Halbjahr.Erstes, stundenGesamt: 14, stundenUnentschuldigt: 3);

        var zeugnisse = new ZeugnisService(db.ContextFactory);
        var ergebnis = await zeugnisse.GetOrCreateAsync(kinder[0], "2026/27", Halbjahr.Erstes);

        Assert.True(ergebnis.WurdeErstellt);
        Assert.Equal(14, ergebnis.Zeugnis!.FehlstundenGesamt);
        Assert.Equal(3, ergebnis.Zeugnis.FehlstundenUnentschuldigt);

        // A later correction of the class table flows into the not-yet-edited report.
        await fehlzeiten.SaveAsync(kinder[0], "2026/27", Halbjahr.Erstes, stundenGesamt: 20, stundenUnentschuldigt: 8);
        var again = await zeugnisse.GetOrCreateAsync(kinder[0], "2026/27", Halbjahr.Erstes);

        Assert.False(again.WurdeErstellt);
        Assert.Equal(20, again.Zeugnis!.FehlstundenGesamt);

        // Once the teacher pins a value on the report, the class table no longer overrides it.
        again.Zeugnis.FehlstundenGesamt = 15;
        again.Zeugnis.FehlstundenManuellUeberschrieben = true;
        await zeugnisse.SaveAsync(again.Zeugnis);

        await fehlzeiten.SaveAsync(kinder[0], "2026/27", Halbjahr.Erstes, stundenGesamt: 30, stundenUnentschuldigt: 9);
        var pinned = await zeugnisse.GetOrCreateAsync(kinder[0], "2026/27", Halbjahr.Erstes);
        Assert.Equal(15, pinned.Zeugnis!.FehlstundenGesamt);
    }

    [Fact]
    public async Task GetOrCreate_is_idempotent_and_auto_picks_the_form_by_year_and_term()
    {
        var (db, _, kinder) = await SeedClassAsync();
        await using var _ = db;

        await using (var ctx = db.NewContext())
        {
            ctx.Formulare.AddRange(StandardFormulare.All());
            await ctx.SaveChangesAsync();
        }

        var zeugnisse = new ZeugnisService(db.ContextFactory);
        var first = await zeugnisse.GetOrCreateAsync(kinder[1], "2026/27", Halbjahr.Erstes);
        var second = await zeugnisse.GetOrCreateAsync(kinder[1], "2026/27", Halbjahr.Erstes);

        Assert.Equal(first.Zeugnis!.Id, second.Zeugnis!.Id);
        Assert.Equal(SuggestionReason.AutomatischNachJahrgang, first.FormularQuelle);

        await using var check = db.NewContext();
        var formular = await check.Formulare.SingleAsync(f => f.Id == first.Zeugnis.FormularId);
        Assert.Equal(3, formular.Jahrgang);
        Assert.Equal(Halbjahr.Erstes, formular.GiltFuerHalbjahr);
    }
}
