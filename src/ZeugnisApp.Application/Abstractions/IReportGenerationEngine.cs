using ZeugnisApp.Domain.Entities;

namespace ZeugnisApp.Application.Abstractions;

/// <summary>
/// The report-generation engine (plan §3, §5). Deliberately the <em>only</em> seam between the app and
/// the OOXML document work, so the Sprint 10 "custom template" feature is additive, not a rewrite.
/// Implemented by <c>ZeugnisApp.Reporting</c> in Sprint 6.
/// </summary>
public interface IReportGenerationEngine
{
    /// <summary>
    /// Fills the template linked to <paramref name="zeugnis"/>'s <see cref="Formular"/> with the report
    /// data and writes a new <c>.docx</c> to <paramref name="targetPath"/>. Never mutates the template.
    /// </summary>
    Task<GeneratedDocumentResult> GenerateAsync(
        Zeugnis zeugnis,
        string targetPath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks each mapped free-text field against its calibrated character budget (plan §5.3)
    /// without generating anything — feeds the orange overflow warnings in the editor.
    /// </summary>
    IReadOnlyList<OverflowWarning> CheckOverflow(Zeugnis zeugnis);
}

/// <summary>Outcome of a generation run.</summary>
public sealed record GeneratedDocumentResult(
    bool Success,
    string? FilePath,
    IReadOnlyList<OverflowWarning> Overflows,
    string? ErrorMessage = null);

/// <summary>One free-text field that exceeds its calibrated budget.</summary>
public sealed record OverflowWarning(string FeldName, int IstZeichen, int MaxZeichen)
{
    public int UeberschussZeichen => Math.Max(0, IstZeichen - MaxZeichen);
}
