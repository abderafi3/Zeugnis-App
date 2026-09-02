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
    public void Standardformulare_are_the_schools_real_seven_forms()
    {
        var forms = StandardFormulare.All();
        Assert.Equal(7, forms.Count);

        Assert.Equal(
            new[]
            {
                "Schuleingangsphase – 1. Schulbesuchsjahr",
                "Schuleingangsphase – 2. Schulbesuchsjahr",
                "Klasse 3 – 1. Halbjahr",
                "Klasse 3 – Schuljahresende",
                "Klasse 4 – 1. Halbjahr",
                "Klasse 4 – Schuljahresende",
                "Zeugnis zum Gemeinsamen Lernen",
            },
            forms.Select(f => f.Anzeigename));

        // Schuleingangsphase: no grades, keyed by Schulbesuchsjahr, year-end only.
        var sep1 = forms[0];
        Assert.False(sep1.HatNotenblock);
        Assert.Equal(1, sep1.Jahrgang);
        Assert.Equal(Halbjahr.Zweites, sep1.GiltFuerHalbjahr);
        Assert.True(forms[1].HatVersetzungsvermerk);   // SEP verbleiben/versetzen decision

        // Klasse 3/4: 11 grade rows incl. the three Deutsch sub-competencies.
        var k4hj1 = forms.Single(f => f.Anzeigename == "Klasse 4 – 1. Halbjahr");
        Assert.True(k4hj1.HatNotenblock);
        Assert.True(k4hj1.HatBegruendeteEmpfehlung);
        Assert.False(k4hj1.HatVersetzungsvermerk);
        Assert.Equal(11, k4hj1.Faecher.Count);
        Assert.Contains("Sprachgebrauch", k4hj1.Faecher);
        Assert.Contains("Rechtschreiben", k4hj1.Faecher);

        // Gemeinsames Lernen: zieldifferent, any year, both terms, no grades.
        var gl = forms[^1];
        Assert.True(gl.IstZieldifferent);
        Assert.False(gl.HatNotenblock);
        Assert.Equal(0, gl.Jahrgang);
        Assert.Null(gl.GiltFuerHalbjahr);
    }

    [Fact]
    public void Suggest_gives_a_zieldifferent_child_the_Gemeinsames_Lernen_form()
    {
        var kind = new Kind { Foerderstatus = Foerderstatus.ZieldifferentLernen };

        // SBJ 2, first term — there is no Klasse-2 HJ1 form, but the GL form (Jahrgang 0) fits.
        var result = FormularSuggester.Suggest(StandardFormulare.All(), kind, 2, Halbjahr.Erstes);

        Assert.Equal("Zeugnis zum Gemeinsamen Lernen", result.Formular!.Anzeigename);
    }

    [Fact]
    public void Suggest_never_gives_a_regular_child_the_Jahrgang_zero_form()
    {
        var kind = new Kind { Foerderstatus = Foerderstatus.Regel };

        // Regular child, SBJ 2, first term: no form fits (Schuleingangsphase is year-end only).
        var result = FormularSuggester.Suggest(StandardFormulare.All(), kind, 2, Halbjahr.Erstes);

        Assert.Equal(SuggestionReason.KeinPassendesFormular, result.Reason);
    }

    [Fact]
    public async Task SuggestAsync_uses_the_childs_Schulbesuchsjahr_over_the_class_year()
    {
        await using var db = new SqliteTestDatabase();
        await new DatabaseInitializer(db.ContextFactory).InitializeAsync();

        Guid kindId;
        await using (var ctx = db.NewContext())
        {
            // Jahrgangsübergreifende Klasse: the class "year" is 3, but this child is in their 4th year.
            var klasse = new Klasse { Name = "F1", Jahrgang = 3 };
            var kind = new Kind
            {
                Vorname = "Nael", Nachname = "K", Klasse = klasse,
                Geschlecht = Geschlecht.Maennlich, Schulbesuchsjahr = 4,
            };
            ctx.AddRange(klasse, kind);
            await ctx.SaveChangesAsync();
            kindId = kind.Id;
        }

        var svc = new FormulareService(db.ContextFactory, db.Clock);
        var result = await svc.SuggestAsync(kindId, Halbjahr.Zweites);

        Assert.Equal(SuggestionReason.AutomatischNachJahrgang, result.Reason);
        Assert.Equal("Klasse 4 – Schuljahresende", result.Formular!.Anzeigename);
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
