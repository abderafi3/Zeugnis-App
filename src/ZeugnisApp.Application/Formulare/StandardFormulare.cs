using ZeugnisApp.Domain.Entities;
using ZeugnisApp.Domain.Enums;

namespace ZeugnisApp.Application.Formulare;

/// <summary>
/// Grundschule Wehringhausen's real report forms (from
/// <c>Zeugnisformulare_GS Wehringhausen_gesamt.docx</c>), as editable metadata. Infrastructure
/// seeds them into an empty database; the reporting tests iterate them. The actual <c>.docx</c>
/// content-control mapping is linked later (Sprint 6).
///
/// The set:
/// <list type="number">
///   <item>Schuleingangsphase – 1. Schulbesuchsjahr — Bericht, Schuljahresende.</item>
///   <item>Schuleingangsphase – 2. Schulbesuchsjahr — Bericht, with the SEP verbleiben/versetzen decision.</item>
///   <item>Klasse 3 – 1. Halbjahr — Noten + Bericht.</item>
///   <item>Klasse 3 – Schuljahresende — Noten + Bericht + Versetzungsvermerk.</item>
///   <item>Klasse 4 – 1. Halbjahr — Noten + Bericht + begründete Schulformempfehlung.</item>
///   <item>Klasse 4 – Schuljahresende — Noten + Bericht + Versetzungsvermerk.</item>
///   <item>Zeugnis zum Gemeinsamen Lernen — Bericht, zieldifferent, any year / both terms.</item>
/// </list>
/// A separate <b>Lern- und Förderempfehlung</b> sheet exists in the source document; it is a
/// per-subject supplement, not a report card, and is modelled on its own later.
/// </summary>
public static class StandardFormulare
{
    // Grade rows on the Klasse 3 / Klasse 4 forms, in the order printed on the real document.
    // The three Deutsch sub-competencies are separate grades under the overall Deutsch line; their
    // names ("Deutsch Sprachgebrauch" …) map straight onto the template tags
    // (Note_Deutsch_Sprachgebrauch) via FieldMapping.
    private static readonly string[] Notenfaecher =
    [
        "Religionslehre",
        "Deutsch", "Deutsch Sprachgebrauch", "Deutsch Lesen", "Deutsch Rechtschreiben",
        "Englisch",
        "Sachunterricht", "Mathematik", "Sport", "Musik", "Kunst",
    ];

    // The narrative sections common to the Schuleingangsphase and Klasse 3/4 forms. The real
    // document splits the Lernentwicklung text across two pages ("Teil 1" / "Teil 2").
    private static readonly string[] BerichtFelder =
        ["Arbeits- und Sozialverhalten", "Lernentwicklung (Teil 1)", "Lernentwicklung (Teil 2)"];

    // The Gemeinsames-Lernen form carries one undivided Lernentwicklung section.
    private static readonly string[] GemeinsamesLernenFelder =
        ["Arbeits- und Sozialverhalten", "Lernentwicklung"];

    public static IReadOnlyList<Formular> All() =>
    [
        Bericht("Schuleingangsphase – 1. Schulbesuchsjahr", 1, Halbjahr.Zweites),
        Bericht("Schuleingangsphase – 2. Schulbesuchsjahr", 2, Halbjahr.Zweites, versetzung: true),
        Noten("Klasse 3 – 1. Halbjahr", 3, Halbjahr.Erstes),
        Noten("Klasse 3 – Schuljahresende", 3, Halbjahr.Zweites, versetzung: true),
        Noten("Klasse 4 – 1. Halbjahr", 4, Halbjahr.Erstes, empfehlung: true),
        Noten("Klasse 4 – Schuljahresende", 4, Halbjahr.Zweites, versetzung: true),
        GemeinsamesLernen(),
    ];

    private static Formular Bericht(string name, int jahrgang, Halbjahr halbjahr, bool versetzung = false) => new()
    {
        Anzeigename = name,
        Ueberschrift = "Zeugnis der Grundschule",
        Jahrgang = jahrgang,
        GiltFuerHalbjahr = halbjahr,
        HatNotenblock = false,
        HatVersetzungsvermerk = versetzung,
        Faecher = [],
        Freitextfelder = ToFelder(BerichtFelder),
    };

    private static Formular Noten(string name, int jahrgang, Halbjahr halbjahr, bool empfehlung = false, bool versetzung = false) => new()
    {
        Anzeigename = name,
        Ueberschrift = "Zeugnis der Grundschule",
        Jahrgang = jahrgang,
        GiltFuerHalbjahr = halbjahr,
        HatNotenblock = true,
        HatBegruendeteEmpfehlung = empfehlung,
        HatVersetzungsvermerk = versetzung,
        Faecher = [.. Notenfaecher],
        Freitextfelder = ToFelder(BerichtFelder),
    };

    private static Formular GemeinsamesLernen() => new()
    {
        Anzeigename = "Zeugnis zum Gemeinsamen Lernen",
        Ueberschrift = "Zeugnis zum Gemeinsamen Lernen an der Grundschule",
        Jahrgang = 0,               // 0 = any year — only suggested for zieldifferente Kinder
        GiltFuerHalbjahr = null,    // issued in both terms
        HatNotenblock = false,
        IstZieldifferent = true,
        Faecher = [],
        Freitextfelder = ToFelder(GemeinsamesLernenFelder),
    };

    // Placeholder character budgets (plan §5.3). ESTIMATES — recalibrate against the real
    // templates by generating test documents and checking the fit (Sprint 6 note).
    private static readonly Dictionary<string, int> Budgets = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Arbeits- und Sozialverhalten"] = 1200,
        ["Lernentwicklung (Teil 1)"] = 1600,
        ["Lernentwicklung (Teil 2)"] = 1600,
        ["Lernentwicklung"] = 2600,
    };

    private static List<FreitextFeldDefinition> ToFelder(IReadOnlyList<string> namen) =>
        [.. namen.Select((n, i) => new FreitextFeldDefinition
        {
            Name = n,
            Reihenfolge = i,
            MaxZeichen = Budgets.TryGetValue(n, out var budget) ? budget : null,
        })];
}
