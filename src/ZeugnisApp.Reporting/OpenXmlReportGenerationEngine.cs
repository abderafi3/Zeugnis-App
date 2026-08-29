using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Serilog;
using ZeugnisApp.Application.Abstractions;
using ZeugnisApp.Application.Reporting;
using ZeugnisApp.Reporting.Ooxml;

namespace ZeugnisApp.Reporting;

/// <summary>
/// Fills the official <c>.docx</c> templates by editing the real OOXML in place — never through
/// Word (plan §5). Only the text inside content controls changes; everything else is byte-independent
/// identical, verified by <see cref="OoxmlCanonicalizer"/> (plan §5.4).
/// </summary>
public sealed class OpenXmlReportGenerationEngine : IReportGenerationEngine
{
    public Task<GeneratedDocumentResult> GenerateAsync(
        ReportRenderRequest request,
        string targetPath,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var templatePath = request.Formular.WordFilePath;
        if (string.IsNullOrWhiteSpace(templatePath) || !File.Exists(templatePath))
        {
            return Task.FromResult(GeneratedDocumentResult.Failed(
                "Für dieses Formular ist keine Word-Vorlage hinterlegt."));
        }

        try
        {
            var directory = Path.GetDirectoryName(targetPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Work on a copy — the template is never opened for editing (plan §5.2).
            File.Copy(templatePath, targetPath, overwrite: true);

            var mapping = FieldMapping.FromJson(request.Formular.FieldMappingJson);
            var values = ReportValues.Build(request);
            var valuesByTag = BuildTagMap(mapping, values);

            using (var doc = WordprocessingDocument.Open(targetPath, isEditable: true))
            {
                var main = doc.MainDocumentPart
                           ?? throw new InvalidOperationException("Die Vorlage enthält kein Hauptdokument.");

                ContentControlFiller.Fill(main.Document, valuesByTag);
                foreach (var header in main.HeaderParts)
                {
                    ContentControlFiller.Fill(header.Header, valuesByTag);
                }

                foreach (var footer in main.FooterParts)
                {
                    ContentControlFiller.Fill(footer.Footer, valuesByTag);
                }

                main.Document.Save();
            }

            return Task.FromResult(new GeneratedDocumentResult(
                Success: true,
                FilePath: targetPath,
                Overflows: CheckOverflow(request)));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Zeugnis-Generierung fehlgeschlagen für {Formular}", request.Formular.Anzeigename);
            TryDelete(targetPath);
            return Task.FromResult(GeneratedDocumentResult.Failed(
                "Das Dokument konnte nicht erzeugt werden: " + ex.Message));
        }
    }

    public IReadOnlyList<OverflowWarning> CheckOverflow(ReportRenderRequest request)
    {
        var warnings = new List<OverflowWarning>();
        var texteByFeld = request.Zeugnis.Freitexte.ToDictionary(f => f.FeldName, f => f.Text, StringComparer.Ordinal);

        foreach (var feld in request.Formular.Freitextfelder)
        {
            if (feld.MaxZeichen is not { } max || max <= 0)
            {
                continue;
            }

            var laenge = texteByFeld.TryGetValue(feld.Name, out var text) ? text.Trim().Length : 0;
            if (laenge > max)
            {
                warnings.Add(new OverflowWarning(feld.Name, laenge, max));
            }
        }

        return warnings;
    }

    private static Dictionary<string, string> BuildTagMap(FieldMapping mapping, IReadOnlyDictionary<string, string> values)
    {
        var byTag = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (valueKey, text) in values)
        {
            if (mapping.TagFor(valueKey) is { } tag)
            {
                byTag[tag] = text;
            }
        }

        return byTag;
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
            // best effort
        }
    }
}
