using Microsoft.EntityFrameworkCore;
using ZeugnisApp.Application;
using ZeugnisApp.Application.Abstractions;
using ZeugnisApp.Application.Formulare;
using ZeugnisApp.Domain.Entities;
using ZeugnisApp.Domain.Enums;
using ZeugnisApp.Infrastructure.Services;

namespace ZeugnisApp.Infrastructure.Tests;

public class FormulareTests
{
    private static readonly Formular[] SevenForms =
    [
        F("K1 Ende", 1, Halbjahr.Zweites),
        F("K2 HJ1", 2, Halbjahr.Erstes),
        F("K2 Ende", 2, Halbjahr.Zweites),
        F("K3 HJ1", 3, Halbjahr.Erstes, noten: true),
        F("K3 Ende", 3, Halbjahr.Zweites, noten: true),
        F("K4 HJ1", 4, Halbjahr.Erstes, noten: true, empfehlung: true),
        F("K4 Ende", 4, Halbjahr.Zweites, noten: true, versetzung: true),
    ];

    private static Formular F(string name, int jg, Halbjahr hj, bool noten = false, bool empfehlung = false, bool versetzung = false, bool zieldifferent = false)
        => new()
        {
            Anzeigename = name, Jahrgang = jg, GiltFuerHalbjahr = hj,
            HatNotenblock = noten, HatBegruendeteEmpfehlung = empfehlung,
            HatVersetzungsvermerk = versetzung, IstZieldifferent = zieldifferent,
        };

    [Theory]
    [InlineData(3, Halbjahr.Erstes, "K3 HJ1")]
    [InlineData(4, Halbjahr.Zweites, "K4 Ende")]
    [InlineData(1, Halbjahr.Zweites, "K1 Ende")]
    public void Suggest_matches_by_year_group_and_term(int jahrgang, Halbjahr halbjahr, string expected)
    {
        var kind = new Kind { Vorname = "Max", Nachname = "M", Foerderstatus = Foerderstatus.Regel };
        var result = FormularSuggester.Suggest(SevenForms, kind, jahrgang, halbjahr);

        Assert.Equal(SuggestionReason.AutomatischNachJahrgang, result.Reason);
        Assert.Equal(expected, result.Formular!.Anzeigename);
    }

    [Fact]
    public void Suggest_prefers_a_zieldifferent_form_for_a_zieldifferent_child()
    {
        var forms = SevenForms.Append(F("K3 HJ1 zieldifferent", 3, Halbjahr.Erstes, zieldifferent: true)).ToArray();
        var kind = new Kind { Foerderstatus = Foerderstatus.ZieldifferentLernen };

        var result = FormularSuggester.Suggest(forms, kind, 3, Halbjahr.Erstes);
        Assert.Equal("K3 HJ1 zieldifferent", result.Formular!.Anzeigename);
    }

    [Fact]
    public void Suggest_falls_back_to_the_regular_form_when_no_zieldifferent_one_exists()
    {
        var kind = new Kind { Foerderstatus = Foerderstatus.ZieldifferentGeistigeEntwicklung };
        var result = FormularSuggester.Suggest(SevenForms, kind, 2, Halbjahr.Erstes);
        Assert.Equal("K2 HJ1", result.Formular!.Anzeigename);
    }

    [Fact]
    public void Suggest_respects_a_manual_override()
    {
        var pinned = SevenForms[6]; // K4 Ende
        var kind = new Kind { Foerderstatus = Foerderstatus.Regel, ManuellesFormularId = pinned.Id };

        var result = FormularSuggester.Suggest(SevenForms, kind, 2, Halbjahr.Erstes);
        Assert.Equal(SuggestionReason.ManuellFestgelegt, result.Reason);
        Assert.Same(pinned, result.Formular);
    }

    [Fact]
    public void Suggest_returns_none_when_nothing_fits()
    {
        var kind = new Kind { Foerderstatus = Foerderstatus.Regel };
        var result = FormularSuggester.Suggest(SevenForms, kind, 9, Halbjahr.Erstes);
        Assert.Equal(SuggestionReason.KeinPassendesFormular, result.Reason);
        Assert.Null(result.Formular);
    }

    [Fact]
    public async Task Standard_seven_forms_are_seeded_once()
    {
        await using var db = new SqliteTestDatabase();
        var init = new DatabaseInitializer(db.ContextFactory);
        await init.InitializeAsync();

        var svc = new FormulareService(db.ContextFactory, db.Clock);
        var all = await svc.GetAllAsync();
        Assert.Equal(7, all.Count);
        Assert.All(all, f => Assert.False(f.IstGenerierungsbereit)); // no .docx linked yet

        // Deleting one and re-initialising does not re-seed.
        await svc.DeleteAsync(all[0].Id);
        await init.InitializeAsync();
        Assert.Equal(6, (await svc.GetAllAsync()).Count);
    }

    [Fact]
    public async Task SaveAsync_replaces_the_owned_freitextfelder()
    {
        await using var db = new SqliteTestDatabase();
        await db.MigrateAsync();
        var svc = new FormulareService(db.ContextFactory, db.Clock);

        var formular = await svc.SaveAsync(new Formular
        {
            Anzeigename = "Test", Jahrgang = 3, GiltFuerHalbjahr = Halbjahr.Erstes,
            Freitextfelder = [new() { Name = "A" }, new() { Name = "B" }],
        });

        formular.Freitextfelder.Clear();
        formular.Freitextfelder.Add(new FreitextFeldDefinition { Name = "C", MaxZeichen = 500 });
        await svc.SaveAsync(formular);

        var reloaded = await svc.GetAsync(formular.Id);
        Assert.Single(reloaded!.Freitextfelder);
        Assert.Equal("C", reloaded.Freitextfelder[0].Name);
        Assert.Equal(500, reloaded.Freitextfelder[0].MaxZeichen);
    }

    [Fact]
    public async Task SaveAsync_rejects_duplicate_field_names()
    {
        await using var db = new SqliteTestDatabase();
        await db.MigrateAsync();
        var svc = new FormulareService(db.ContextFactory, db.Clock);

        await Assert.ThrowsAsync<EntityValidationException>(() => svc.SaveAsync(new Formular
        {
            Anzeigename = "Test", Jahrgang = 1,
            Freitextfelder = [new() { Name = "Deutsch" }, new() { Name = "deutsch" }],
        }));
    }
}
