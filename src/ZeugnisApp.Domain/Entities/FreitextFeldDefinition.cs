namespace ZeugnisApp.Domain.Entities;

/// <summary>
/// Defines one free-text field on a <see cref="Formular"/> (plan §4). Owned by the template.
/// </summary>
public class FreitextFeldDefinition
{
    /// <summary>Stable field key, also used to look up the value on a <see cref="Zeugnis"/> and to map to a content-control tag.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Label / helper text shown above the editor box.</summary>
    public string? Hinweistext { get; set; }

    /// <summary>Display order in the editor.</summary>
    public int Reihenfolge { get; set; }

    /// <summary>
    /// Overflow budget: maximum characters that still fit the real template without pushing a page.
    /// Calibrated manually per template (plan §5.3). <c>null</c> = not yet calibrated (no warning shown).
    /// </summary>
    public int? MaxZeichen { get; set; }
}
