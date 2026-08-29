using ZeugnisApp.Application.Formulare;
using ZeugnisApp.Domain.Entities;
using ZeugnisApp.Domain.Enums;

namespace ZeugnisApp.Application.Abstractions;

/// <summary>
/// Report-card lifecycle (plan §Sprint 7 auto-transfer; the full editor is Sprint 8/9).
/// </summary>
public interface IZeugnisService
{
    /// <summary>
    /// Returns the report card for this child + term, creating it if absent. On creation the
    /// template is auto-suggested (manual override respected) and the current absence totals are
    /// copied in — still editable per report afterwards (plan §Sprint 7 DoD).
    /// </summary>
    Task<ZeugnisErgebnis> GetOrCreateAsync(
        Guid kindId, string schuljahr, Halbjahr halbjahr, Guid? formularIdOverride = null,
        CancellationToken cancellationToken = default);

    Task<Zeugnis?> GetAsync(Guid zeugnisId, CancellationToken cancellationToken = default);
}

/// <summary>Outcome of <see cref="IZeugnisService.GetOrCreateAsync"/>.</summary>
public sealed record ZeugnisErgebnis(Zeugnis? Zeugnis, bool WurdeErstellt, SuggestionReason? FormularQuelle, string? Fehler)
{
    public bool Erfolg => Zeugnis is not null;
}
