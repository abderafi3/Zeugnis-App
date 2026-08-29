using Microsoft.EntityFrameworkCore;
using ZeugnisApp.Application;
using ZeugnisApp.Domain.Entities;
using ZeugnisApp.Domain.Enums;
using ZeugnisApp.Infrastructure.Services;

namespace ZeugnisApp.Infrastructure.Tests;

public class KlassenServiceTests
{
    private static async Task<(SqliteTestDatabase db, KlassenService svc)> NewAsync()
    {
        var db = new SqliteTestDatabase();
        await db.MigrateAsync();
        return (db, new KlassenService(db.ContextFactory, db.Clock));
    }

    [Fact]
    public async Task SaveKlasseAsync_inserts_then_updates_the_same_row()
    {
        var (db, svc) = await NewAsync();
        await using var _ = db;

        var klasse = new Klasse { Name = "E1", Jahrgang = 1, Klassenleitung = "Frau A" };
        var saved = await svc.SaveKlasseAsync(klasse);

        saved.Name = "E1a";
        saved.Wochenstundenplan.Montag = 5;
        await svc.SaveKlasseAsync(saved);

        var all = await svc.GetKlassenAsync();
        Assert.Single(all);
        Assert.Equal("E1a", all[0].Name);
    }

    [Fact]
    public async Task SaveKlasseAsync_rejects_invalid_class()
    {
        var (db, svc) = await NewAsync();
        await using var _ = db;

        var ex = await Assert.ThrowsAsync<EntityValidationException>(
            () => svc.SaveKlasseAsync(new Klasse { Name = "", Jahrgang = 9 }));

        Assert.Contains(nameof(Klasse.Name), ex.Errors.Keys);
        Assert.Contains(nameof(Klasse.Jahrgang), ex.Errors.Keys);
    }

    [Fact]
    public async Task GetKlassenAsync_reports_child_count_and_incomplete_count()
    {
        var (db, svc) = await NewAsync();
        await using var _ = db;

        var klasse = await svc.SaveKlasseAsync(new Klasse { Name = "3a", Jahrgang = 3 });

        await svc.SaveKindAsync(new Kind
        {
            KlasseId = klasse.Id, Vorname = "Max", Nachname = "Mustermann",
            Geburtsdatum = new DateOnly(2017, 3, 1), Geschlecht = Geschlecht.Maennlich,
        });
        await svc.SaveKindAsync(new Kind { KlasseId = klasse.Id, Vorname = "Erika", Nachname = "Musterfrau" });

        var item = Assert.Single(await svc.GetKlassenAsync());
        Assert.Equal(2, item.KinderAnzahl);
        Assert.Equal(1, item.UnvollstaendigeKinder);
    }

    [Fact]
    public async Task DeleteKlasseAsync_soft_deletes_class_children_and_their_reports()
    {
        var (db, svc) = await NewAsync();
        await using var _ = db;

        var klasse = await svc.SaveKlasseAsync(new Klasse { Name = "4b", Jahrgang = 4 });
        var kind = await svc.SaveKindAsync(new Kind { KlasseId = klasse.Id, Vorname = "Max", Nachname = "M" });

        await using (var ctx = db.NewContext())
        {
            var formular = new Formular { Anzeigename = "F" };
            ctx.Formulare.Add(formular);
            ctx.Zeugnisse.Add(new Zeugnis { KindId = kind.Id, FormularId = formular.Id, Schuljahr = "2026/27", Halbjahr = Halbjahr.Erstes });
            await ctx.SaveChangesAsync();
        }

        await svc.DeleteKlasseAsync(klasse.Id);

        Assert.Empty(await svc.GetKlassenAsync());
        Assert.Empty(await svc.GetKinderAsync(klasse.Id));

        await using (var ctx = db.NewContext())
        {
            Assert.All(await ctx.Klassen.IgnoreQueryFilters().ToListAsync(), k => Assert.NotNull(k.DeletedUtc));
            Assert.All(await ctx.Kinder.IgnoreQueryFilters().ToListAsync(), k => Assert.NotNull(k.DeletedUtc));
            Assert.All(await ctx.Zeugnisse.IgnoreQueryFilters().ToListAsync(), z => Assert.NotNull(z.DeletedUtc));
        }
    }

    [Fact]
    public async Task DeleteKindAsync_soft_deletes_only_that_child()
    {
        var (db, svc) = await NewAsync();
        await using var _ = db;

        var klasse = await svc.SaveKlasseAsync(new Klasse { Name = "1a", Jahrgang = 1 });
        var a = await svc.SaveKindAsync(new Kind { KlasseId = klasse.Id, Vorname = "A", Nachname = "A" });
        await svc.SaveKindAsync(new Kind { KlasseId = klasse.Id, Vorname = "B", Nachname = "B" });

        await svc.DeleteKindAsync(a.Id);

        var remaining = await svc.GetKinderAsync(klasse.Id);
        Assert.Single(remaining);
        Assert.Equal("B", remaining[0].Vorname);
    }

    [Fact]
    public async Task SaveKindAsync_rejects_foerderschwerpunkt_for_regel_children()
    {
        var (db, svc) = await NewAsync();
        await using var _ = db;

        var klasse = await svc.SaveKlasseAsync(new Klasse { Name = "2a", Jahrgang = 2 });

        await Assert.ThrowsAsync<EntityValidationException>(() => svc.SaveKindAsync(new Kind
        {
            KlasseId = klasse.Id, Vorname = "Max", Nachname = "M",
            Foerderstatus = Foerderstatus.Regel, Foerderschwerpunkt = "Lernen",
        }));
    }
}
