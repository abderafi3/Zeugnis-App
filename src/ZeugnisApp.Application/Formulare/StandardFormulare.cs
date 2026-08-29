using ZeugnisApp.Domain.Entities;
using ZeugnisApp.Domain.Enums;

namespace ZeugnisApp.Application.Formulare;

/// <summary>
/// The seven standard NRW Grundschule report templates (plan §Sprint 5). Pure definitions —
/// Infrastructure seeds them into the database, the reporting tests iterate them.
/// </summary>
public static class StandardFormulare
{
    private static readonly string[] BerichtFelder =
        ["Deutsch", "Mathematik", "Sachunterricht", "Englisch", "Arbeits- und Sozialverhalten", "Bemerkungen"];

    private static readonly string[] NotenFelder =
        ["Arbeits- und Sozialverhalten", "Bemerkungen"];

    private static readonly string[] Notenfaecher =
        ["Deutsch", "Mathematik", "Sachunterricht", "Englisch", "Kunst", "Musik", "Sport", "Religionslehre"];

    public static IReadOnlyList<Formular> All() =>
    [
        Bericht("Klasse 1 – Schuljahresende", 1, Halbjahr.Zweites),
        Bericht("Klasse 2 – 1. Halbjahr", 2, Halbjahr.Erstes),
        Bericht("Klasse 2 – Schuljahresende", 2, Halbjahr.Zweites),
        Noten("Klasse 3 – 1. Halbjahr", 3, Halbjahr.Erstes),
        Noten("Klasse 3 – Schuljahresende", 3, Halbjahr.Zweites),
        Noten("Klasse 4 – 1. Halbjahr", 4, Halbjahr.Erstes, empfehlung: true),
        Noten("Klasse 4 – Schuljahresende", 4, Halbjahr.Zweites, versetzung: true),
    ];

    private static Formular Bericht(string name, int jahrgang, Halbjahr halbjahr) => new()
    {
        Anzeigename = name,
        Ueberschrift = "Zeugnis der Grundschule",
        Jahrgang = jahrgang,
        GiltFuerHalbjahr = halbjahr,
        HatNotenblock = false,
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
        Freitextfelder = ToFelder(NotenFelder),
    };

    private static List<FreitextFeldDefinition> ToFelder(IReadOnlyList<string> namen) =>
        [.. namen.Select((n, i) => new FreitextFeldDefinition { Name = n, Reihenfolge = i })];
}
