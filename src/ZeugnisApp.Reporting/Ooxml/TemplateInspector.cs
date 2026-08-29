using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ZeugnisApp.Application.Abstractions;

namespace ZeugnisApp.Reporting.Ooxml;

/// <summary>Lists the content controls in an uploaded template (plan §Sprint 10 field-mapping wizard).</summary>
public sealed class TemplateInspector : ITemplateInspector
{
    public IReadOnlyList<ContentControlInfo> ReadContentControls(string docxPath)
    {
        if (!File.Exists(docxPath))
        {
            throw new InvalidTemplateException("Die Datei wurde nicht gefunden.");
        }

        WordprocessingDocument doc;
        try
        {
            doc = WordprocessingDocument.Open(docxPath, isEditable: false);
        }
        catch (Exception ex) when (ex is OpenXmlPackageException or FileFormatException or InvalidDataException)
        {
            throw new InvalidTemplateException("Die Datei ist keine gültige Word-Datei (.docx).");
        }

        using (doc)
        {
            var main = doc.MainDocumentPart
                       ?? throw new InvalidTemplateException("Die Datei enthält kein Word-Hauptdokument.");

            var found = new List<ContentControlInfo>();
            var seen = new HashSet<string>(StringComparer.Ordinal);

            Collect(main.Document, "Text", found, seen);
            foreach (var header in main.HeaderParts)
            {
                Collect(header.Header, "Kopfzeile", found, seen);
            }

            foreach (var footer in main.FooterParts)
            {
                Collect(footer.Footer, "Fußzeile", found, seen);
            }

            return found;
        }
    }

    private static void Collect(OpenXmlElement root, string location, List<ContentControlInfo> found, HashSet<string> seen)
    {
        foreach (var sdt in root.Descendants<SdtElement>())
        {
            var props = sdt.SdtProperties;
            var tag = props?.GetFirstChild<Tag>()?.Val?.Value;
            if (string.IsNullOrWhiteSpace(tag) || !seen.Add(tag))
            {
                continue;
            }

            var alias = props?.GetFirstChild<SdtAlias>()?.Val?.Value;
            var sample = string.Concat(sdt.Descendants<Text>().Select(t => t.Text)).Trim();
            if (sample.Length > 60)
            {
                sample = sample[..60] + "…";
            }

            found.Add(new ContentControlInfo(tag, alias, sample, location));
        }
    }
}
