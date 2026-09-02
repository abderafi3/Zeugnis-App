using ZeugnisApp.Domain.Common;
using ZeugnisApp.Domain.Enums;

namespace ZeugnisApp.Domain.Entities;

/// <summary>
/// A pupil (plan §4). <see cref="Foerderstatus"/> plus the class year drive automatic template
/// suggestion; <see cref="ManuellesFormularId"/> lets a teacher override that choice.
/// </summary>
public class Kind : EntityBase, ISoftDeletable
{
    public string Vorname { get; set; } = string.Empty;
    public string Nachname { get; set; } = string.Empty;
    public DateOnly? Geburtsdatum { get; set; }
    public Geschlecht? Geschlecht { get; set; }

    public Guid KlasseId { get; set; }
    public Klasse? Klasse { get; set; }

    /// <summary>
    /// School-visit year 1–4 (Schulbesuchsjahr). Grundschule Wehringhausen runs
    /// jahrgangsübergreifende Klassen (E1, E7 …) that mix years, so the child's own
    /// Schulbesuchsjahr — not <see cref="Klasse.Jahrgang"/> — drives report-template suggestion
    /// and phrase filtering. 0 = not set → falls back to the class year.
    /// </summary>
    public int Schulbesuchsjahr { get; set; }

    public Foerderstatus Foerderstatus { get; set; } = Foerderstatus.Regel;

    /// <summary>Free text; only meaningful when <see cref="Foerderstatus"/> is not <see cref="Foerderstatus.Regel"/>.</summary>
    public string? Foerderschwerpunkt { get; set; }

    /// <summary>Manual template override. When set, template auto-suggestion is bypassed.</summary>
    public Guid? ManuellesFormularId { get; set; }
    public Formular? ManuellesFormular { get; set; }

    public DateTime? DeletedUtc { get; set; }

    /// <summary>True when the mandatory fields for a usable record are all present (plan §Sprint 2 red badge).</summary>
    public bool IstVollstaendig =>
        !string.IsNullOrWhiteSpace(Vorname)
        && !string.IsNullOrWhiteSpace(Nachname)
        && Geburtsdatum is not null
        && Geschlecht is not null;

    public string AnzeigeName => $"{Vorname} {Nachname}".Trim();

    /// <summary>
    /// The year that drives template suggestion and phrase filtering: the child's own
    /// <see cref="Schulbesuchsjahr"/> when set (1–4), otherwise the class's
    /// <see cref="Klasse.Jahrgang"/> (0 when neither is known). Requires <see cref="Klasse"/>
    /// to be loaded for the fallback.
    /// </summary>
    public int EffektivesSchulbesuchsjahr =>
        Schulbesuchsjahr is >= 1 and <= 4 ? Schulbesuchsjahr : Klasse?.Jahrgang ?? 0;
}
