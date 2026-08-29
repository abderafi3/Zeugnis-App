using ZeugnisApp.Domain.Common;
using ZeugnisApp.Domain.Enums;

namespace ZeugnisApp.Domain.Entities;

/// <summary>
/// One report card for one child, on one template, in one term (plan §4 — central transactional entity).
/// Unique on (<see cref="KindId"/>, <see cref="FormularId"/>, <see cref="Schuljahr"/>, <see cref="Halbjahr"/>),
/// enforced by the database (plan §4).
/// </summary>
public class Zeugnis : EntityBase, ISoftDeletable
{
    public Guid KindId { get; set; }
    public Kind? Kind { get; set; }

    public Guid FormularId { get; set; }
    public Formular? Formular { get; set; }

    public string Schuljahr { get; set; } = string.Empty;
    public Halbjahr Halbjahr { get; set; }

    /// <summary>Free-text values keyed by <see cref="FreitextFeldDefinition.Name"/> (owned).</summary>
    public List<FreitextEintrag> Freitexte { get; set; } = [];

    /// <summary>Grades keyed by subject (owned). Only used when the template has a grade block.</summary>
    public List<NoteEintrag> Noten { get; set; } = [];

    // --- Jahrgang 4: weiterführende Schule ---
    public string? Empfehlung { get; set; }
    public string? EmpfehlungBegruendung { get; set; }

    // --- Absence numbers, copied from Fehlzeit at creation, editable afterwards (plan §Sprint 7) ---
    public int? FehlstundenGesamt { get; set; }
    public int? FehlstundenUnentschuldigt { get; set; }

    public string? VersetzungsvermerkText { get; set; }
    public string? Bemerkungen { get; set; }

    public bool Fertiggestellt { get; set; }

    /// <summary>Path of the most recently generated <c>.docx</c>, or null if never generated.</summary>
    public string? GeneratedFilePath { get; set; }

    public DateTime? DeletedUtc { get; set; }
}

/// <summary>Owned: one free-text answer on a <see cref="Zeugnis"/>.</summary>
public class FreitextEintrag
{
    public string FeldName { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}

/// <summary>Owned: one subject grade on a <see cref="Zeugnis"/>.</summary>
public class NoteEintrag
{
    public string Fach { get; set; } = string.Empty;

    /// <summary>Grade as text ("sehr gut", "2", "teilgenommen", "—"). Null = not yet graded.</summary>
    public string? Note { get; set; }
}
