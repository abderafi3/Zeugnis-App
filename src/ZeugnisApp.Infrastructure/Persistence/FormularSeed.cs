using ZeugnisApp.Domain.Entities;
using ZeugnisApp.Domain.Enums;

namespace ZeugnisApp.Infrastructure.Persistence;

/// <summary>
/// The seven standard NRW Grundschule report templates (plan §Sprint 5). Seeded once into an empty
/// database so a teacher starts with editable metadata; the real <c>.docx</c> files are linked in
/// Sprint 6. Deleting them does not re-seed.
/// </summary>
internal static class FormularSeed
{
    private static readonly string[] BerichtFreitext =
        ["Deutsch", "Mathematik", "Sachunterricht", "Englisch", "Arbeits- und Sozialverhalten", "Bemerkungen"];

    private static readonly string[] NotenFreitext =
        ["Arbeits- und Sozialverhalten", "Bemerkungen"];

    private static readonly string[] Notenfaecher =
        ["Deutsch", "Mathematik", "Sachunterricht", "Englisch", "Kunst", "Musik", "Sport", "Evangelische/Katholische Religionslehre"];

    public static IReadOnlyList<Formular> Standardformulare() =>
    [
        Bericht("Klasse 1 – Schuljahresende", jahrgang: 1, halbjahr: Halbjahr.Zweites),
        Bericht("Klasse 2 – 1. Halbjahr", jahrgang: 2, halbjahr: Halbjahr.Erstes),
        Bericht("Klasse 2 – Schuljahresende", jahrgang: 2, halbjahr: Halbjahr.Zweites),
        Noten("Klasse 3 – 1. Halbjahr", jahrgang: 3, halbjahr: Halbjahr.Erstes),
        Noten("Klasse 3 – Schuljahresende", jahrgang: 3, halbjahr: Halbjahr.Zweites),
        Noten("Klasse 4 – 1. Halbjahr", jahrgang: 4, halbjahr: Halbjahr.Erstes, empfehlung: true),
        Noten("Klasse 4 – Schuljahresende", jahrgang: 4, halbjahr: Halbjahr.Zweites, versetzung: true),
    ];

    private static Formular Bericht(string name, int jahrgang, Halbjahr halbjahr) => new()
    {
        Anzeigename = name,
        Ueberschrift = "Zeugnis der Grundschule",
        Jahrgang = jahrgang,
        GiltFuerHalbjahr = halbjahr,
        HatNotenblock = false,
        Faecher = [],
        Freitextfelder = ToFelder(BerichtFreitext),
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
        Freitextfelder = ToFelder(NotenFreitext),
    };

    private static List<FreitextFeldDefinition> ToFelder(IReadOnlyList<string> namen) =>
        [.. namen.Select((n, i) => new FreitextFeldDefinition { Name = n, Reihenfolge = i })];
}
