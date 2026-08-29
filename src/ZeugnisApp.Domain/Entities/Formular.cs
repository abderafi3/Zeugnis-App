using ZeugnisApp.Domain.Common;

namespace ZeugnisApp.Domain.Entities;

/// <summary>
/// A report-card template (plan §4). Sprints 5–7 configure the 7 official forms; Sprint 10 lets a
/// teacher add their own. The <see cref="WordFilePath"/> + <see cref="FieldMappingJson"/> pair links
/// the app's fields to content-control tags in the real <c>.docx</c> (plan §5).
/// </summary>
public class Formular : EntityBase, ISoftDeletable
{
    public string Anzeigename { get; set; } = string.Empty;

    /// <summary>Heading printed on the document (e.g. "Zeugnis der Grundschule — Klasse 3").</summary>
    public string? Ueberschrift { get; set; }

    /// <summary>Subjects shown as a grade block, in order. Empty when <see cref="HatNotenblock"/> is false.</summary>
    public List<string> Faecher { get; set; } = [];

    /// <summary>Free-text field definitions (owned).</summary>
    public List<FreitextFeldDefinition> Freitextfelder { get; set; } = [];

    // --- Switches (plan §4 / §Sprint 5) ---
    public bool HatNotenblock { get; set; }
    public bool HatBegruendeteEmpfehlung { get; set; }
    public bool HatVersetzungsvermerk { get; set; }
    public bool IstZieldifferent { get; set; }

    /// <summary>Absolute or app-relative path to the content-controlled official <c>.docx</c>. Null until linked (Sprint 6).</summary>
    public string? WordFilePath { get; set; }

    /// <summary>JSON map of app field key → content-control tag. Populated in Sprint 6/10.</summary>
    public string? FieldMappingJson { get; set; }

    public DateTime? DeletedUtc { get; set; }

    /// <summary>A template is usable for generation once a Word file has been linked.</summary>
    public bool IstGenerierungsbereit => !string.IsNullOrWhiteSpace(WordFilePath);
}
