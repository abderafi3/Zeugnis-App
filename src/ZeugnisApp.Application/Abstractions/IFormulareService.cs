using ZeugnisApp.Application.Formulare;
using ZeugnisApp.Domain.Entities;
using ZeugnisApp.Domain.Enums;

namespace ZeugnisApp.Application.Abstractions;

/// <summary>Template metadata management + auto-suggestion (plan §Sprint 5). No Word-file work — that is Sprint 6.</summary>
public interface IFormulareService
{
    Task<IReadOnlyList<FormularListItem>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Formular?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Formular> SaveAsync(Formular formular, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Proposes the template that fits a child in a given term (manual override respected).</summary>
    Task<FormularSuggestion> SuggestAsync(Guid kindId, Halbjahr halbjahr, CancellationToken cancellationToken = default);

    /// <summary>
    /// Copies <paramref name="sourceDocxPath"/> into the app's template folder, links it to the
    /// Formular and stores the field mapping (plan §Sprint 10). Returns the stored path.
    /// </summary>
    Task<string> LinkTemplateAsync(
        Guid formularId, string sourceDocxPath, string fieldMappingJson, CancellationToken cancellationToken = default);
}

/// <summary>Row in the template overview (plan §7.2).</summary>
public sealed record FormularListItem(
    Guid Id,
    string Anzeigename,
    int Jahrgang,
    Halbjahr? GiltFuerHalbjahr,
    bool IstZieldifferent,
    bool IstGenerierungsbereit);
