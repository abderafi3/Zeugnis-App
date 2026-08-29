using ZeugnisApp.Domain.Enums;

namespace ZeugnisApp.Application.Zeugnisse;

/// <summary>Well-known picklist values for the report-card editor (plan §Sprint 8).</summary>
public static class ZeugnisDefaults
{
    /// <summary>Standard grade words used in NRW Grundschule report cards.</summary>
    public static readonly IReadOnlyList<string> Noten =
    [
        "sehr gut", "gut", "befriedigend", "ausreichend", "mangelhaft", "ungenügend",
        "teilgenommen", "—",
    ];

    /// <summary>Weiterführende Schulformen for the begründete Empfehlung (Jahrgang 4).</summary>
    public static readonly IReadOnlyList<string> Schulformempfehlungen =
    [
        "Gymnasium", "Gesamtschule", "Sekundarschule", "Realschule", "Hauptschule",
    ];

    public static readonly IReadOnlyList<string> Versetzungsvermerke =
    [
        "wird in die nächste Klasse versetzt",
        "wird nicht versetzt",
        "rückt gemäß § 11 SchulG auf",
    ];
}

/// <summary>
/// The mandatory remark for zieldifferent taught pupils (plan §Sprint 8 — "legally-required
/// special-support text auto-insertion"). Wording is a sensible default; confirm the exact
/// phrasing with the school.
/// </summary>
public static class ZieldifferentHinweis
{
    public const string Lernen =
        "Die Schülerin / der Schüler wurde zieldifferent im Förderschwerpunkt Lernen unterrichtet. " +
        "Die Leistungen werden auf der Grundlage eines individuellen Förderplans bewertet.";

    public const string GeistigeEntwicklung =
        "Die Schülerin / der Schüler wurde zieldifferent im Förderschwerpunkt Geistige Entwicklung " +
        "unterrichtet. Die Leistungen werden auf der Grundlage eines individuellen Förderplans beschrieben.";

    public static string? For(Foerderstatus status) => status switch
    {
        Foerderstatus.ZieldifferentLernen => Lernen,
        Foerderstatus.ZieldifferentGeistigeEntwicklung => GeistigeEntwicklung,
        _ => null,
    };
}
