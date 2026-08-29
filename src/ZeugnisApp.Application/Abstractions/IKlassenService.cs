using ZeugnisApp.Domain.Entities;

namespace ZeugnisApp.Application.Abstractions;

/// <summary>
/// Class and pupil management (plan §Sprint 2). Deleting is a soft-delete: the aggregate and its
/// children move to the Papierkorb (restore UI arrives in Sprint 11).
/// </summary>
public interface IKlassenService
{
    Task<IReadOnlyList<KlasseListItem>> GetKlassenAsync(CancellationToken cancellationToken = default);

    Task<Klasse?> GetKlasseAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Inserts a new class or updates an existing one. Validates first (throws <see cref="EntityValidationException"/>).</summary>
    Task<Klasse> SaveKlasseAsync(Klasse klasse, CancellationToken cancellationToken = default);

    /// <summary>Soft-deletes the class and cascades the soft-delete to its Kinder, Fehlzeiten and Zeugnisse.</summary>
    Task DeleteKlasseAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Kind>> GetKinderAsync(Guid klasseId, CancellationToken cancellationToken = default);

    Task<Kind?> GetKindAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Kind> SaveKindAsync(Kind kind, CancellationToken cancellationToken = default);

    Task DeleteKindAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Options for the manual template override on a child (plan §4).</summary>
    Task<IReadOnlyList<FormularOption>> GetFormularOptionenAsync(CancellationToken cancellationToken = default);
}

/// <summary>Row shown in the class overview (plan §7.2).</summary>
public sealed record KlasseListItem(
    Guid Id,
    string Name,
    int Jahrgang,
    string Klassenleitung,
    int KinderAnzahl,
    int UnvollstaendigeKinder);

public sealed record FormularOption(Guid Id, string Anzeigename);
