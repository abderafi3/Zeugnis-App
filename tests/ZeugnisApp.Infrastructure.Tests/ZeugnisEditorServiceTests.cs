using Microsoft.EntityFrameworkCore;
using ZeugnisApp.Application.Formulare;
using ZeugnisApp.Application.Zeugnisse;
using ZeugnisApp.Domain.Entities;
using ZeugnisApp.Domain.Enums;
using ZeugnisApp.Infrastructure.Services;

namespace ZeugnisApp.Infrastructure.Tests;

public class ZeugnisEditorServiceTests
{
    private static async Task<(SqliteTestDatabase db, Guid klasseId, Guid annaId, Guid tomId)> SeedAsync(
        Foerderstatus tomStatus = Foerderstatus.Regel)
    {
        var db = new SqliteTestDatabase();
        var init = new DatabaseInitializer(db.ContextFactory);
        await init.InitializeAsync(); // seeds the 7 standard forms

        await using var ctx = db.NewContext();
        var klasse = new Klasse { Name = "3a", Jahrgang = 3 };
        var anna = new Kind { Vorname = "Anna", Nachname = "A", Klasse = klasse, Foerderstatus = Foerderstatus.Regel };
        var tom = new Kind { Vorname = "Tom", Nachname = "B", Klasse = klasse, Foerderstatus = tomStatus };
        ctx.AddRange(klasse, anna, tom);
        await ctx.SaveChangesAsync();
        return (db, klasse.Id, anna.Id, tom.Id);
    }

    [Fact]
    public async Task SaveAsync_round_trips_the_owned_freitexte_and_noten()
    {
        var (db, _, annaId, _) = await SeedAsync();
        await using var _ = db;
        var svc = new ZeugnisService(db.ContextFactory);

        var zeugnis = (await svc.GetOrCreateAsync(annaId, "2026/27", Halbjahr.Erstes)).Zeugnis!;
        zeugnis.Freitexte.Add(new FreitextEintrag { FeldName = "Bemerkungen", Text = "Sehr engagiert." });
        zeugnis.Noten.Add(new NoteEintrag { Fach = "Deutsch", Note = "gut" });
        zeugnis.Bemerkungen = "Teilnahme an der Chor-AG.";
        await svc.SaveAsync(zeugnis);

        var reloaded = await svc.GetAsync(zeugnis.Id);
        Assert.Equal("Sehr engagiert.", Assert.Single(reloaded!.Freitexte).Text);
        Assert.Equal("gut", Assert.Single(reloaded.Noten).Note);
        Assert.Equal("Teilnahme an der Chor-AG.", reloaded.Bemerkungen);

        // Re-save with fewer entries replaces, not appends.
        reloaded.Noten.Clear();
        await svc.SaveAsync(reloaded);
        Assert.Empty((await svc.GetAsync(zeugnis.Id))!.Noten);
    }

    [Fact]
    public async Task SetFertiggestelltAsync_toggles_the_flag()
    {
        var (db, _, annaId, _) = await SeedAsync();
        await using var _ = db;
        var svc = new ZeugnisService(db.ContextFactory);

        var zeugnis = (await svc.GetOrCreateAsync(annaId, "2026/27", Halbjahr.Erstes)).Zeugnis!;
        await svc.SetFertiggestelltAsync(zeugnis.Id, true);

        Assert.True((await svc.GetAsync(zeugnis.Id))!.Fertiggestellt);
    }

    [Fact]
    public async Task GetUebersicht_shows_a_row_per_child_with_status_and_form()
    {
        var (db, klasseId, annaId, _) = await SeedAsync();
        await using var _ = db;
        var svc = new ZeugnisService(db.ContextFactory);

        var created = (await svc.GetOrCreateAsync(annaId, "2026/27", Halbjahr.Erstes)).Zeugnis!;
        await svc.SetFertiggestelltAsync(created.Id, true);

        var rows = await svc.GetUebersichtForKlasseAsync(klasseId, "2026/27", Halbjahr.Erstes);

        Assert.Equal(2, rows.Count);
        var anna = rows.First(r => r.KindId == annaId);
        Assert.True(anna.Fertiggestellt);
        Assert.NotNull(anna.ZeugnisId);
        Assert.Contains("Klasse 3", anna.FormularName!, StringComparison.Ordinal);

        var tom = rows.First(r => r.KindId != annaId);
        Assert.Null(tom.ZeugnisId);
        Assert.True(tom.FormularVerfuegbar);      // auto-suggested even before the report exists
    }

    [Fact]
    public async Task GetWithContext_loads_the_formular_fields_and_the_kind()
    {
        var (db, _, annaId, _) = await SeedAsync();
        await using var _ = db;
        var svc = new ZeugnisService(db.ContextFactory);

        var id = (await svc.GetOrCreateAsync(annaId, "2026/27", Halbjahr.Erstes)).Zeugnis!.Id;
        var full = await svc.GetWithContextAsync(id);

        Assert.NotNull(full!.Kind);
        Assert.NotNull(full.Formular);
        Assert.NotEmpty(full.Formular!.Freitextfelder);
    }

    [Fact]
    public void ZieldifferentHinweis_returns_the_mandated_text_only_for_zieldifferente_children()
    {
        Assert.Null(ZieldifferentHinweis.For(Foerderstatus.Regel));
        Assert.Null(ZieldifferentHinweis.For(Foerderstatus.Zielgleich));
        Assert.Contains("Lernen", ZieldifferentHinweis.For(Foerderstatus.ZieldifferentLernen)!, StringComparison.Ordinal);
        Assert.Contains("Geistige Entwicklung", ZieldifferentHinweis.For(Foerderstatus.ZieldifferentGeistigeEntwicklung)!, StringComparison.Ordinal);
    }
}
