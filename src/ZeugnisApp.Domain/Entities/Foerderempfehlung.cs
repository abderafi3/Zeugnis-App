using ZeugnisApp.Domain.Common;
using ZeugnisApp.Domain.Enums;

namespace ZeugnisApp.Domain.Entities;

/// <summary>
/// A Lern- und Förderempfehlung (LFE) — the per-subject supplement to a report card that NRW
/// primary schools issue when a child's achievement in a subject is weak or promotion is at risk
/// (source: <c>Zeugnisformulare_GS Wehringhausen_gesamt.docx</c>, last sheet). One row per child,
/// term and subject; unique on (<see cref="KindId"/>, <see cref="Schuljahr"/>,
/// <see cref="Halbjahr"/>, <see cref="Fach"/>).
/// </summary>
public class Foerderempfehlung : EntityBase
{
    public Guid KindId { get; set; }
    public Kind? Kind { get; set; }

    public string Schuljahr { get; set; } = string.Empty;
    public Halbjahr Halbjahr { get; set; }

    /// <summary>The subject the recommendation is for, e.g. "Deutsch", "Mathematik".</summary>
    public string Fach { get; set; } = string.Empty;

    /// <summary>"Beobachtungen" — what was observed about the child's learning in this subject.</summary>
    public string? Beobachtungen { get; set; }

    /// <summary>"Schulische Maßnahmen" — what the school will do to support the child.</summary>
    public string? SchulischeMassnahmen { get; set; }

    /// <summary>"Empfehlungen für Zuhause" — how the family can help.</summary>
    public string? EmpfehlungenFuerZuhause { get; set; }

    public bool Fertiggestellt { get; set; }

    /// <summary>Path of the most recently generated <c>.docx</c>, or null if never generated.</summary>
    public string? GeneratedFilePath { get; set; }

    /// <summary>True once every section carries text.</summary>
    public bool IstInhaltlichVollstaendig =>
        !string.IsNullOrWhiteSpace(Beobachtungen)
        && !string.IsNullOrWhiteSpace(SchulischeMassnahmen)
        && !string.IsNullOrWhiteSpace(EmpfehlungenFuerZuhause);
}
