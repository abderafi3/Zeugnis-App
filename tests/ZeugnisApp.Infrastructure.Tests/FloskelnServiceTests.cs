using System.Text;
using ZeugnisApp.Application;
using ZeugnisApp.Application.Abstractions;
using ZeugnisApp.Application.Import;
using ZeugnisApp.Domain.Entities;
using ZeugnisApp.Domain.Enums;
using ZeugnisApp.Infrastructure.Import;
using ZeugnisApp.Infrastructure.Services;

namespace ZeugnisApp.Infrastructure.Tests;

public class FloskelnServiceTests
{
    private static async Task<(SqliteTestDatabase db, FloskelnService svc)> NewAsync()
    {
        var db = new SqliteTestDatabase();
        await db.MigrateAsync();
        return (db, new FloskelnService(db.ContextFactory, db.Clock));
    }

    [Fact]
    public async Task Filter_by_bereich_stufe_and_search_text()
    {
        var (db, svc) = await NewAsync();
        await using var _ = db;

        await svc.SaveAsync(new Floskel { Text = "[Name] arbeitet ruhig.", Bereich = "Arbeitsverhalten", Stufe = 1, Klassengruppe = Klassengruppe.Eingangsstufe });
        await svc.SaveAsync(new Floskel { Text = "[Name] hilft anderen.", Bereich = "Sozialverhalten", Stufe = 1, Klassengruppe = Klassengruppe.Beide });
        await svc.SaveAsync(new Floskel { Text = "[Name] rechnet sicher.", Bereich = "Mathematik", Stufe = 3, Klassengruppe = Klassengruppe.Folgestufe });

        Assert.Equal(3, (await svc.GetAsync(new FloskelFilter())).Count);
        Assert.Single(await svc.GetAsync(new FloskelFilter(Bereich: "Mathematik")));
        Assert.Equal(2, (await svc.GetAsync(new FloskelFilter(Stufe: 1))).Count);
        Assert.Single(await svc.GetAsync(new FloskelFilter(SearchText: "hilft")));

        // Klassengruppe filter includes "Beide".
        Assert.Equal(2, (await svc.GetAsync(new FloskelFilter(Klassengruppe: Klassengruppe.Eingangsstufe))).Count);
    }

    [Fact]
    public async Task GetMeistgenutzte_matches_the_field_loosely_and_orders_by_usage()
    {
        var (db, svc) = await NewAsync();
        await using var _ = db;

        var selten = await svc.SaveAsync(new Floskel { Text = "[Name] beteiligt sich selten.", Bereich = "Sozialverhalten", Stufe = 3 });
        var oft = await svc.SaveAsync(new Floskel { Text = "[Name] ist hilfsbereit.", Bereich = "Sozialverhalten", Stufe = 3 });
        await svc.SaveAsync(new Floskel { Text = "[Name] rechnet sicher.", Bereich = "Mathematik", Stufe = 3 });
        await svc.SaveAsync(new Floskel { Text = "Falsche Stufe.", Bereich = "Sozialverhalten", Stufe = 1 });

        await svc.RegistriereVerwendungAsync(oft.Id);
        await svc.RegistriereVerwendungAsync(oft.Id);
        await svc.RegistriereVerwendungAsync(selten.Id);

        // Field labelled "Arbeits- und Sozialverhalten" must still catch "Sozialverhalten" phrases.
        var top = await svc.GetMeistgenutzteAsync("Arbeits- und Sozialverhalten", stufe: 3, limit: 5);

        Assert.Equal(2, top.Count);
        Assert.Equal(oft.Id, top[0].Id);       // most used first
        Assert.Equal(selten.Id, top[1].Id);
        Assert.Equal(2, top[0].Verwendungen);
    }

    [Fact]
    public async Task Wildcard_floskel_stays_visible_under_every_filter_and_field()
    {
        var (db, svc) = await NewAsync();
        await using var _ = db;

        // Empty Bereich + Stufe 0 = "gilt für alle".
        var alle = await svc.SaveAsync(new Floskel { Text = "[Name] ist freundlich.", Bereich = "", Stufe = 0 });
        await svc.SaveAsync(new Floskel { Text = "[Name] rechnet sicher.", Bereich = "Mathematik", Stufe = 3 });

        // Shows up when filtering by a specific Bereich...
        var mathe = await svc.GetAsync(new FloskelFilter(Bereich: "Mathematik"));
        Assert.Contains(mathe, f => f.Id == alle.Id);

        // ...and by a specific Stufe.
        var stufe2 = await svc.GetAsync(new FloskelFilter(Stufe: 2));
        Assert.Contains(stufe2, f => f.Id == alle.Id);

        // ...and in the editor's "häufig genutzt" list for any field.
        var top = await svc.GetMeistgenutzteAsync("Deutsch", stufe: 1, limit: 5);
        Assert.Contains(top, f => f.Id == alle.Id);
    }

    [Fact]
    public async Task SaveAsync_rejects_an_empty_text()
    {
        var (db, svc) = await NewAsync();
        await using var _ = db;

        await Assert.ThrowsAsync<EntityValidationException>(
            () => svc.SaveAsync(new Floskel { Text = "", Bereich = "Sozialverhalten", Stufe = 1 }));
    }

    [Fact]
    public async Task GetBereicheAsync_merges_used_and_well_known_values()
    {
        var (db, svc) = await NewAsync();
        await using var _ = db;

        await svc.SaveAsync(new Floskel { Text = "x", Bereich = "Schwimmen", Stufe = 2 });

        var bereiche = await svc.GetBereicheAsync();
        Assert.Contains("Schwimmen", bereiche);
        Assert.Contains(FloskelBereiche.Sozialverhalten, bereiche);
        Assert.Equal(bereiche.Distinct(StringComparer.OrdinalIgnoreCase).Count(), bereiche.Count);
    }

    [Fact]
    public async Task Excel_import_maps_stufe_and_klassengruppe_tokens()
    {
        var (db, svc) = await NewAsync();
        await using var _ = db;

        const string csv =
            "Text;Bereich;Stufe;Klassengruppe\n" +
            "[Name] arbeitet ruhig.;Arbeitsverhalten;1;E\n" +
            "[Name] rechnet sicher.;Mathematik;3;F\n" +
            "[Name] ist hilfsbereit.;Sozialverhalten;2;\n";
        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var session = new ImportSession(new TabularImport().FromCsv(ms), ImportProfiles.Floskeln);
        Assert.True(session.HasHeaderRow);
        Assert.True(session.AllRequiredMapped);

        var data = session.BuildRows().Where(r => r.IsValid).Select(FloskelImportMapper.Map).ToList();
        Assert.Equal(3, data.Count);
        Assert.Equal(Klassengruppe.Eingangsstufe, data[0].Klassengruppe);
        Assert.Equal(Klassengruppe.Folgestufe, data[1].Klassengruppe);
        Assert.Equal(Klassengruppe.Beide, data[2].Klassengruppe);

        var summary = await new FloskelImportService(db.ContextFactory).CommitAsync(data, uebersprungen: 0);
        Assert.Equal(3, summary.Importiert);
        Assert.Equal(3, (await svc.GetAsync(new FloskelFilter())).Count);
    }
}
