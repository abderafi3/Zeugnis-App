using ZeugnisApp.Application.Import;

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
