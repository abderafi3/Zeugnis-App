using ZeugnisApp.Domain.Common;

namespace ZeugnisApp.Domain.Entities;

/// <summary>
/// Calendar-based absence variant (plan §4): one entry per day a child was absent. Modelled and
/// unit-tested but intentionally NOT surfaced in the UI yet — the simple <see cref="Fehlzeit"/>
/// totals are what teachers enter for now.
/// </summary>
public class Fehltag : EntityBase
{
    public Guid KindId { get; set; }
    public Kind? Kind { get; set; }

    public DateOnly Datum { get; set; }

    /// <summary>Lesson hours missed on that day.</summary>
    public int Stunden { get; set; }

    public bool Unentschuldigt { get; set; }

    public string? Bemerkung { get; set; }
}
