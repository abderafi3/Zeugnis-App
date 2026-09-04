using ZeugnisApp.Domain.Entities;
using ZeugnisApp.Domain.Enums;

namespace ZeugnisApp.Application.Abstractions;

/// <summary>
/// Lern- und Förderempfehlungen (LFE) — the per-subject supplement to a report card
/// (source: the school's real form document). One per child, term and subject.
/// </summary>
public interface IFoerderempfehlungService
{
    /// <summary>All recommendations for one child in one term, newest subject first by name.</summary>
    Task<IReadOnlyList<Foerderempfehlung>> GetForKindAsync(
        Guid kindId, string schuljahr, Halbjahr halbjahr, CancellationToken cancellationToken = default);

    Task<Foerderempfehlung?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the recommendation for this child + term + subject, creating an empty one if absent.
    /// </summary>
    Task<Foerderempfehlung> GetOrCreateAsync(
        Guid kindId, string schuljahr, Halbjahr halbjahr, string fach,
        CancellationToken cancellationToken = default);

    /// <summary>Persists the editable text and the <c>Fertiggestellt</c> flag. Validates first.</summary>
    Task SaveAsync(Foerderempfehlung empfehlung, CancellationToken cancellationToken = default);

    /// <summary>Hard-deletes the recommendation.</summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
