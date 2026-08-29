using ZeugnisApp.Domain.Common;

namespace ZeugnisApp.Domain.Entities;

/// <summary>
/// A school class (plan §4). Deleting a class cascade-deletes its children, absence records
/// and report cards — the UI confirms first (plan §Sprint 2).
/// </summary>
public class Klasse : EntityBase, ISoftDeletable
{
    /// <summary>Display name, e.g. "E1", "3a".</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Year group, 1–4.</summary>
    public int Jahrgang { get; set; }

    /// <summary>Name of the class teacher (Klassenleitung).</summary>
    public string Klassenleitung { get; set; } = string.Empty;

    /// <summary>Optional per-class report date; falls back to <see cref="SchoolSettings.Zeugnisausgabedatum"/> when null.</summary>
    public DateOnly? Zeugnisdatum { get; set; }

    /// <summary>Owned — never null; a fresh plan is all-zero.</summary>
    public Wochenstundenplan Wochenstundenplan { get; set; } = new();

    public DateTime? DeletedUtc { get; set; }

    public ICollection<Kind> Kinder { get; } = new List<Kind>();
}
