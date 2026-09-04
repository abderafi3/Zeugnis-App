using ZeugnisApp.Domain.Common;
using ZeugnisApp.Domain.Enums;

namespace ZeugnisApp.Domain.Entities;

/// <summary>
/// A reusable text snippet (plan §4). <see cref="Text"/> may contain placeholders in either the
/// app's own bracket syntax (<c>[Name]</c>, <c>[sein/ihr]</c>) or the school's native syntax
/// (<c>$Vorname$</c>, <c>&amp;Er%Sie&amp;</c>), both resolved per child by the
/// <c>PlaceholderEngine</c> (plan §Sprint 4).
/// </summary>
public class Floskel : EntityBase, ISoftDeletable
{
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// The school's stable identifier from its phrase spreadsheet (e.g. <c>#D11.00</c>).
    /// Empty for phrases a teacher adds by hand. Used to de-duplicate on re-import.
    /// </summary>
    public string? Kuerzel { get; set; }

    /// <summary>Category (e.g. "Sozialverhalten", "Deutsch"). Free text so Excel import stays forgiving. See <see cref="FloskelBereiche"/>.</summary>
    public string Bereich { get; set; } = string.Empty;

    /// <summary>Year group the phrase is written for: 1–4, or 0 for "gilt für alle Jahrgänge".</summary>
    public int Stufe { get; set; }

    /// <summary>
    /// Differentiation level from the school's phrase file (column "Niveau"): 1 = strong … 5 = in
    /// need of support; 0 = neutral / no differentiation. Lets a teacher pick the wording that
    /// matches the child's achievement level.
    /// </summary>
    public int Niveau { get; set; }

    public Klassengruppe Klassengruppe { get; set; } = Klassengruppe.Beide;

    /// <summary>
    /// How often this phrase has been inserted from the Zeugnis-Editor. Drives the
    /// "häufig genutzt" shortcut list next to each free-text field (plan §7.2).
    /// </summary>
    public int Verwendungen { get; set; }

    public DateTime? DeletedUtc { get; set; }
}

/// <summary>
/// Well-known <see cref="Floskel.Bereich"/> values, aligned with Grundschule Wehringhausen's
/// phrase file and report forms. Not exhaustive — the field is free text.
/// </summary>
public static class FloskelBereiche
{
    public const string ArbeitsUndSozialverhalten = "Arbeits- und Sozialverhalten";
    public const string Arbeitsverhalten = "Arbeitsverhalten";
    public const string Sozialverhalten = "Sozialverhalten";
    public const string Deutsch = "Deutsch";
    public const string Sprachgebrauch = "Sprachgebrauch";
    public const string Lesen = "Lesen";
    public const string Rechtschreiben = "Rechtschreiben";
    public const string Mathematik = "Mathematik";
    public const string Sachunterricht = "Sachunterricht";
    public const string Englisch = "Englisch";
    public const string Kunst = "Kunst";
    public const string Musik = "Musik";
    public const string Sport = "Sport";
    public const string EvangelischeReligionslehre = "Evangelische Religionslehre";
    public const string KatholischeReligionslehre = "Katholische Religionslehre";
    public const string Foerderschwerpunkt = "Förderschwerpunkt";
    public const string Bemerkungen = "Bemerkungen";

    public static readonly IReadOnlyList<string> Alle =
    [
        ArbeitsUndSozialverhalten, Arbeitsverhalten, Sozialverhalten,
        Deutsch, Sprachgebrauch, Lesen, Rechtschreiben,
        Mathematik, Sachunterricht, Englisch, Kunst, Musik, Sport,
        EvangelischeReligionslehre, KatholischeReligionslehre,
        Foerderschwerpunkt, Bemerkungen,
    ];
}
