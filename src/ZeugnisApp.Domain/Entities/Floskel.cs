using ZeugnisApp.Domain.Common;
using ZeugnisApp.Domain.Enums;

namespace ZeugnisApp.Domain.Entities;

/// <summary>
/// A reusable text snippet (plan §4). <see cref="Text"/> may contain the placeholders
/// <c>[Name]</c> and <c>[sein/ihr]</c>, resolved per child by the placeholder engine (plan §Sprint 4).
/// </summary>
public class Floskel : EntityBase, ISoftDeletable
{
    public string Text { get; set; } = string.Empty;

    /// <summary>Category (e.g. "Sozialverhalten", "Deutsch"). Free text so Excel import stays forgiving. See <see cref="FloskelBereiche"/>.</summary>
    public string Bereich { get; set; } = string.Empty;

    /// <summary>Year group the phrase is written for, 1–4.</summary>
    public int Stufe { get; set; }

    public Klassengruppe Klassengruppe { get; set; } = Klassengruppe.Beide;

    /// <summary>
    /// How often this phrase has been inserted from the Zeugnis-Editor. Drives the
    /// "häufig genutzt" shortcut list next to each free-text field (plan §7.2).
    /// </summary>
    public int Verwendungen { get; set; }

    public DateTime? DeletedUtc { get; set; }
}

/// <summary>Well-known <see cref="Floskel.Bereich"/> values. Not exhaustive — the field is free text.</summary>
public static class FloskelBereiche
{
    public const string Arbeitsverhalten = "Arbeitsverhalten";
    public const string Sozialverhalten = "Sozialverhalten";
    public const string Deutsch = "Deutsch";
    public const string Mathematik = "Mathematik";
    public const string Sachunterricht = "Sachunterricht";
    public const string Englisch = "Englisch";
    public const string Kunst = "Kunst";
    public const string Musik = "Musik";
    public const string Sport = "Sport";
    public const string Religion = "Religion";
    public const string Bemerkungen = "Bemerkungen";

    public static readonly IReadOnlyList<string> Alle =
    [
        Arbeitsverhalten, Sozialverhalten, Deutsch, Mathematik, Sachunterricht,
        Englisch, Kunst, Musik, Sport, Religion, Bemerkungen,
    ];
}
