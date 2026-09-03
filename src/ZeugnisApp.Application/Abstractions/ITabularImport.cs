using ZeugnisApp.Application.Import;
using ZeugnisApp.Domain.Enums;

namespace ZeugnisApp.Application.Abstractions;

/// <summary>
/// Parses the four supported import sources into the shared <see cref="TabularGrid"/> (plan §6, step 1).
/// Implemented in Infrastructure (ClosedXML / OpenXML SDK / CsvHelper / delimiter sniffing).
/// </summary>
public interface ITabularImport
{
    TabularGrid FromExcel(Stream stream);

    TabularGrid FromWordTable(Stream stream);

    TabularGrid FromCsv(Stream stream);

    /// <summary>
    /// A tabular PDF export (e.g. SchILD's "Schüler-Abwesenheit – Zusammenfassung"). Rows are
    /// rebuilt from the text word positions; a name that wraps onto a second line is rejoined.
    /// </summary>
    TabularGrid FromPdf(Stream stream);

    /// <summary>Pasted clipboard text — tab, semicolon or comma separated.</summary>
    TabularGrid FromDelimitedText(string text);
}

/// <summary>Commits a validated class-list import (plan §6, step 4).</summary>
public interface IKinderImportService
{
    Task<ImportSummary> CommitAsync(
        Guid klasseId,
        IReadOnlyList<KindImportData> rows,
        int uebersprungen,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Commits a validated absence import: matches each row's name onto a child already in the class
/// and upserts that child's totals for the term (plan §Sprint 7). Names that match no child are
/// reported back, not created.
/// </summary>
public interface IFehlzeitenImportService
{
    Task<FehlzeitenImportErgebnis> CommitAsync(
        Guid klasseId,
        string schuljahr,
        Halbjahr halbjahr,
        IReadOnlyList<FehlzeitImportData> rows,
        int uebersprungen,
        CancellationToken cancellationToken = default);
}

/// <summary>Outcome of an absence import — like <see cref="ImportSummary"/> but naming the misses.</summary>
public sealed record FehlzeitenImportErgebnis(
    int Importiert,
    int Uebersprungen,
    IReadOnlyList<string> NichtZugeordnet)
{
    public ImportSummary ToSummary() => new(Importiert, Uebersprungen);
}
