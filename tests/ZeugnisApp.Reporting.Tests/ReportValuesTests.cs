using ZeugnisApp.Application.Abstractions;
using ZeugnisApp.Application.Reporting;
using ZeugnisApp.Domain.Entities;
using ZeugnisApp.Domain.Enums;

namespace ZeugnisApp.Reporting.Tests;

public class ReportValuesTests
{
    private static ReportRenderRequest Request(Zeugnis zeugnis) => new(
        zeugnis,
        new Kind { Vorname = "Erika", Nachname = "Mustermann" },
        new Klasse { Name = "F1", Jahrgang = 3 },
        new Formular { Anzeigename = "Klasse 3 – Schuljahresende" },
        new SchoolSettings { Name = "GS Wehringhausen" });

    [Fact]
    public void Versetzungsvermerk_is_folded_into_the_Bemerkungen_value()
    {
        var values = ReportValues.Build(Request(new Zeugnis
        {
            Schuljahr = "2026/27",
            Halbjahr = Halbjahr.Zweites,
            Bemerkungen = "Erika hat an der Theater-AG teilgenommen.",
            VersetzungsvermerkText = "Erika wird in die Klasse 4 versetzt.",
        }));

        Assert.Equal(
            "Erika hat an der Theater-AG teilgenommen.\n\nErika wird in die Klasse 4 versetzt.",
            values[ValueKeys.ZeugnisBemerkungen]);

        // still available on its own key for templates that map it separately
        Assert.Equal("Erika wird in die Klasse 4 versetzt.", values[ValueKeys.ZeugnisVersetzungsvermerk]);
    }

    [Fact]
    public void Bemerkungen_value_is_unchanged_when_there_is_no_Versetzungsvermerk()
    {
        var values = ReportValues.Build(Request(new Zeugnis
        {
            Schuljahr = "2026/27", Halbjahr = Halbjahr.Erstes,
            Bemerkungen = "Nur eine Bemerkung.",
        }));

        Assert.Equal("Nur eine Bemerkung.", values[ValueKeys.ZeugnisBemerkungen]);
    }

    [Fact]
    public void Bemerkungen_value_is_just_the_Versetzungsvermerk_when_Bemerkungen_is_empty()
    {
        var values = ReportValues.Build(Request(new Zeugnis
        {
            Schuljahr = "2026/27", Halbjahr = Halbjahr.Zweites,
            VersetzungsvermerkText = "Erika wird versetzt.",
        }));

        Assert.Equal("Erika wird versetzt.", values[ValueKeys.ZeugnisBemerkungen]);
    }

    [Fact]
    public void Grades_and_free_texts_are_keyed_by_their_names()
    {
        var values = ReportValues.Build(Request(new Zeugnis
        {
            Schuljahr = "2026/27", Halbjahr = Halbjahr.Zweites,
            Noten = [new NoteEintrag { Fach = "Deutsch Sprachgebrauch", Note = "gut" }],
            Freitexte = [new FreitextEintrag { FeldName = "Lernentwicklung (Teil 1)", Text = "Text." }],
        }));

        Assert.Equal("gut", values[ValueKeys.Note("Deutsch Sprachgebrauch")]);
        Assert.Equal("Text.", values[ValueKeys.Freitext("Lernentwicklung (Teil 1)")]);
    }
}
