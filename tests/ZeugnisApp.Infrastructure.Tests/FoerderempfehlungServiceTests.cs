using ZeugnisApp.Application;
using ZeugnisApp.Domain.Entities;
using ZeugnisApp.Domain.Enums;
using ZeugnisApp.Infrastructure.Services;

namespace ZeugnisApp.Infrastructure.Tests;

public class FoerderempfehlungServiceTests
{
    private static async Task<(SqliteTestDatabase db, FoerderempfehlungService svc, Guid kindId)> NewAsync()
    {
        var db = new SqliteTestDatabase();
        await db.MigrateAsync();

        Guid kindId;
        await using (var ctx = db.NewContext())
        {
            var klasse = new Klasse { Name = "F1", Jahrgang = 3 };
            var kind = new Kind { Vorname = "Tom", Nachname = "B", Klasse = klasse, Schulbesuchsjahr = 3 };
            ctx.AddRange(klasse, kind);
            await ctx.SaveChangesAsync();
            kindId = kind.Id;
        }

        return (db, new FoerderempfehlungService(db.ContextFactory), kindId);
    }

    [Fact]
    public async Task GetOrCreate_creates_once_then_returns_the_same_row()
    {
        var (db, svc, kindId) = await NewAsync();
        await using var _ = db;

        var first = await svc.GetOrCreateAsync(kindId, "2026/27", Halbjahr.Erstes, "Deutsch");
        var again = await svc.GetOrCreateAsync(kindId, "2026/27", Halbjahr.Erstes, " Deutsch ");

        Assert.Equal(first.Id, again.Id);
        Assert.Equal("Deutsch", again.Fach);
        Assert.Single(await svc.GetForKindAsync(kindId, "2026/27", Halbjahr.Erstes));
    }

    [Fact]
    public async Task Save_persists_the_three_sections_and_the_done_flag()
    {
        var (db, svc, kindId) = await NewAsync();
        await using var _ = db;

        var lfe = await svc.GetOrCreateAsync(kindId, "2026/27", Halbjahr.Erstes, "Mathematik");
        lfe.Beobachtungen = "  Rechnet im ZR bis 100 noch unsicher.  ";
        lfe.SchulischeMassnahmen = "Fördergruppe zweimal wöchentlich.";
        lfe.EmpfehlungenFuerZuhause = "Täglich 10 Minuten Kopfrechnen üben.";
        lfe.Fertiggestellt = true;
        await svc.SaveAsync(lfe);

        var reloaded = await svc.GetAsync(lfe.Id);
        Assert.Equal("Rechnet im ZR bis 100 noch unsicher.", reloaded!.Beobachtungen);   // trimmed
        Assert.Equal("Fördergruppe zweimal wöchentlich.", reloaded.SchulischeMassnahmen);
        Assert.True(reloaded.Fertiggestellt);
        Assert.True(reloaded.IstInhaltlichVollstaendig);
    }

    [Fact]
    public async Task GetForKind_lists_one_row_per_subject_ordered_by_subject()
    {
        var (db, svc, kindId) = await NewAsync();
        await using var _ = db;

        await svc.GetOrCreateAsync(kindId, "2026/27", Halbjahr.Erstes, "Mathematik");
        await svc.GetOrCreateAsync(kindId, "2026/27", Halbjahr.Erstes, "Deutsch");
        await svc.GetOrCreateAsync(kindId, "2026/27", Halbjahr.Zweites, "Deutsch");   // other term

        var hj1 = await svc.GetForKindAsync(kindId, "2026/27", Halbjahr.Erstes);
        Assert.Equal(new[] { "Deutsch", "Mathematik" }, hj1.Select(e => e.Fach));
    }

    [Fact]
    public async Task Save_rejects_an_empty_subject()
    {
        var (db, svc, kindId) = await NewAsync();
        await using var _ = db;

        await Assert.ThrowsAsync<EntityValidationException>(() => svc.SaveAsync(new Foerderempfehlung
        {
            KindId = kindId, Schuljahr = "2026/27", Halbjahr = Halbjahr.Erstes, Fach = "   ",
        }));
    }

    [Fact]
    public async Task Delete_removes_the_row()
    {
        var (db, svc, kindId) = await NewAsync();
        await using var _ = db;

        var lfe = await svc.GetOrCreateAsync(kindId, "2026/27", Halbjahr.Erstes, "Englisch");
        await svc.DeleteAsync(lfe.Id);

        Assert.Null(await svc.GetAsync(lfe.Id));
        Assert.Empty(await svc.GetForKindAsync(kindId, "2026/27", Halbjahr.Erstes));
    }
}
