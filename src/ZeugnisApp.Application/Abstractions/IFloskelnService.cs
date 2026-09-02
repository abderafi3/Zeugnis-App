using ZeugnisApp.Application.Import;
using ZeugnisApp.Domain.Entities;
using ZeugnisApp.Domain.Enums;

namespace ZeugnisApp.Application.Abstractions;

/// <summary>Phrase-library management (plan §Sprint 4).</summary>
public interface IFloskelnService
{
    Task<IReadOnlyList<Floskel>> GetAsync(FloskelFilter filter, CancellationToken cancellationToken = default);

    Task<Floskel> SaveAsync(Floskel floskel, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Distinct Bereich values currently in use, merged with the well-known ones, for the filter.</summary>
    Task<IReadOnlyList<string>> GetBereicheAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// The phrases most often inserted for a given free-text field, matched loosely by Bereich and
    /// year group, most-used first. Feeds the "häufig genutzt" shortcuts in the Zeugnis-Editor.
    /// </summary>
    Task<IReadOnlyList<Floskel>> GetMeistgenutzteAsync(
        string feldName, int stufe, int limit, CancellationToken cancellationToken = default);

    /// <summary>Records one insertion of a phrase from the Zeugnis-Editor (bumps its usage count).</summary>
    Task RegistriereVerwendungAsync(Guid floskelId, CancellationToken cancellationToken = default);
}

/// <summary>Filter for the phrase-library screen. A null field means "all".</summary>
public sealed record FloskelFilter(
    string? Bereich = null,
    int? Stufe = null,
    int? Niveau = null,
    Klassengruppe? Klassengruppe = null,
    string? SearchText = null);

/// <summary>Commits a phrase-library import (plan §Sprint 4, reuses the Sprint 3 pipeline).</summary>
public interface IFloskelImportService
{
    Task<ImportSummary> CommitAsync(
        IReadOnlyList<FloskelImportData> rows,
        int uebersprungen,
        CancellationToken cancellationToken = default);
}
