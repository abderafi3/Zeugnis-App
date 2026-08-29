namespace ZeugnisApp.Application.Abstractions;

/// <summary>Reads the content controls out of an uploaded <c>.docx</c> (plan §Sprint 10). Implemented in <c>ZeugnisApp.Reporting</c>.</summary>
public interface ITemplateInspector
{
    /// <summary>
    /// Opens <paramref name="docxPath"/> and lists every content control (body, headers, footers).
    /// Throws <see cref="InvalidTemplateException"/> if the file is not a readable OOXML <c>.docx</c>.
    /// </summary>
    IReadOnlyList<ContentControlInfo> ReadContentControls(string docxPath);
}

/// <summary>One content control found in a template.</summary>
/// <param name="Tag">The <c>w:tag</c> value — the stable key the engine fills by.</param>
/// <param name="Alias">The <c>w:alias</c> (friendly name shown in Word), if any.</param>
/// <param name="SampleText">The current placeholder text, trimmed, for the teacher to recognise it.</param>
/// <param name="Location">Where it sits: "Text", "Kopfzeile" or "Fußzeile".</param>
public sealed record ContentControlInfo(string Tag, string? Alias, string SampleText, string Location)
{
    public string Anzeige => string.IsNullOrWhiteSpace(Alias) ? Tag : $"{Alias} ({Tag})";
}

/// <summary>The uploaded file is not a usable content-controlled <c>.docx</c>.</summary>
public sealed class InvalidTemplateException(string message) : Exception(message);
