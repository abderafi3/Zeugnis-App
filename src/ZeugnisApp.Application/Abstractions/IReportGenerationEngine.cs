using ZeugnisApp.Domain.Entities;

namespace ZeugnisApp.Application.Abstractions;

/// <summary>
/// The report-generation engine (plan §3, §5). The <em>only</em> seam between the app and the OOXML
/// document work, so the Sprint 10 "custom template" feature is additive, not a rewrite.
/// Implemented by <c>ZeugnisApp.Reporting</c>.
/// </summary>
public interface IReportGenerationEngine
{
    /// <summary>
    /// Fills the template linked to <see cref="ReportRenderRequest.Formular"/> with the report data
    /// and writes a new <c>.docx</c> to <paramref name="targetPath"/>. Never mutates the template:
    /// only the text inside content controls is replaced (plan §5.2).
    /// </summary>
    Task<GeneratedDocumentResult> GenerateAsync(
        ReportRenderRequest request,
        string targetPath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks each free-text field against its calibrated character budget (plan §5.3) without
    /// generating anything — feeds the orange overflow warnings in the editor.
    /// </summary>
    IReadOnlyList<OverflowWarning> CheckOverflow(ReportRenderRequest request);
}

/// <summary>Everything the engine needs to render one report card. Assembled by the caller (Sprint 9).</summary>
public sealed record ReportRenderRequest(
    Zeugnis Zeugnis,
    Kind Kind,
    Klasse Klasse,
    Formular Formular,
    SchoolSettings Schule);

/// <summary>Outcome of a generation run.</summary>
public sealed record GeneratedDocumentResult(
    bool Success,
    string? FilePath,
    IReadOnlyList<OverflowWarning> Overflows,
    string? ErrorMessage = null)
{
    public static GeneratedDocumentResult Failed(string message)
        => new(false, null, [], message);
}

/// <summary>One free-text field that exceeds its calibrated budget.</summary>
public sealed record OverflowWarning(string FeldName, int IstZeichen, int MaxZeichen)
{
    public int UeberschussZeichen => Math.Max(0, IstZeichen - MaxZeichen);
}
