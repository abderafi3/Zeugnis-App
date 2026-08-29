using ZeugnisApp.Domain.Common;
using ZeugnisApp.Domain.Enums;

namespace ZeugnisApp.Domain.Entities;

/// <summary>
/// Absence totals for one child in one term (plan §4, "simple table variant").
/// The calendar-based variant is modelled separately and left unlinked from the UI (plan §Sprint 7).
/// </summary>
public class Fehlzeit : EntityBase
{
    public Guid KindId { get; set; }
    public Kind? Kind { get; set; }

    public string Schuljahr { get; set; } = string.Empty;
    public Halbjahr Halbjahr { get; set; }

    public int StundenGesamt { get; set; }
    public int StundenUnentschuldigt { get; set; }

    /// <summary>Domain rule: unexcused hours can never exceed the total (plan §Sprint 7 inline validation).</summary>
    public bool IstGueltig => StundenUnentschuldigt >= 0
        && StundenGesamt >= 0
        && StundenUnentschuldigt <= StundenGesamt;
}
