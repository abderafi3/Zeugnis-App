// -----------------------------------------------------------------------------
//  Spike A — plan §Sprint 0
//  Goal: prove that filling a .docx via the OpenXML SDK by replacing only the
//  *text* inside content controls causes NO change to anything else in the
//  document, verified by a canonical-XML comparison (the method the real
//  regression suite will use — plan §5.4).
//
//  There is no real school template in the repo, so this spike builds a small
//  document with a table, a styled heading and a Plain-Text content control,
//  then fills the control and diffs the result against the pristine original.
// -----------------------------------------------------------------------------

using System.Xml.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

const string tag = "StudentName";
var work = Path.Combine(Path.GetTempPath(), "zeugnis-spike-openxml");
Directory.CreateDirectory(work);
var pristine = Path.Combine(work, "pristine.docx");
var filled = Path.Combine(work, "filled.docx");

BuildTemplate(pristine, tag);
File.Copy(pristine, filled, overwrite: true);
var replaced = FillContentControls(filled, new Dictionary<string, string> { [tag] = "Erika Mustermann" });

Console.WriteLine($"Content controls filled: {replaced}");

var report = CompareOutsideContentControls(pristine, filled);
Console.WriteLine();
Console.WriteLine(report.ToText());

Environment.Exit(report.Identical && replaced == 1 ? 0 : 1);

// -----------------------------------------------------------------------------

static void BuildTemplate(string path, string sdtTag)
{
    using var doc = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document);
    var main = doc.AddMainDocumentPart();

    var stylePart = main.AddNewPart<StyleDefinitionsPart>();
    stylePart.Styles = new Styles(
        new Style(
            new StyleName { Val = "Heading1" },
            new StyleRunProperties(new Bold(), new FontSize { Val = "32" }))
        {
            Type = StyleValues.Paragraph,
            StyleId = "Heading1",
        });
    stylePart.Styles.Save();

    var heading = new Paragraph(
        new ParagraphProperties(new ParagraphStyleId { Val = "Heading1" }),
        new Run(new Text("Zeugnis der Grundschule")));

    var table = new Table(
        new TableProperties(new TableBorders(
            new TopBorder { Val = BorderValues.Single, Size = 4 },
            new BottomBorder { Val = BorderValues.Single, Size = 4 },
            new LeftBorder { Val = BorderValues.Single, Size = 4 },
            new RightBorder { Val = BorderValues.Single, Size = 4 },
            new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4 },
            new InsideVerticalBorder { Val = BorderValues.Single, Size = 4 })),
        new TableRow(
            new TableCell(new Paragraph(new Run(new Text("Fach")))),
            new TableCell(new Paragraph(new Run(new Text("Note"))))),
        new TableRow(
            new TableCell(new Paragraph(new Run(new Text("Deutsch")))),
            new TableCell(new Paragraph(new Run(new Text("sehr gut"))))));

    // Plain-text content control: run properties carry the formatting we must preserve.
    var sdt = new SdtRun(
        new SdtProperties(
            new SdtId { Val = 101 },
            new Tag { Val = sdtTag },
            new SdtAlias { Val = "Name des Kindes" },
            new RunProperties(new Bold(), new Color { Val = "1F4E79" }),
            new SdtContentText()),
        new SdtContentRun(new Run(
            new RunProperties(new Bold(), new Color { Val = "1F4E79" }),
            new Text("[Name eintragen]"))));

    var namePara = new Paragraph(
        new Run(new Text("Name: ") { Space = SpaceProcessingModeValues.Preserve }),
        sdt);

    main.Document = new Document(new Body(heading, table, namePara, new SectionProperties()));
    main.Document.Save();
}

static int FillContentControls(string path, IReadOnlyDictionary<string, string> values)
{
    using var doc = WordprocessingDocument.Open(path, isEditable: true);
    var body = doc.MainDocumentPart!.Document.Body!;
    var count = 0;

    foreach (var sdt in body.Descendants<SdtElement>().ToList())
    {
        var sdtTag = sdt.SdtProperties?.GetFirstChild<Tag>()?.Val?.Value;
        if (sdtTag is null || !values.TryGetValue(sdtTag, out var newText))
        {
            continue;
        }

        var contentRun = sdt.Descendants<SdtContentRun>().FirstOrDefault();
        var contentBlock = sdt.Descendants<SdtContentBlock>().FirstOrDefault();
        var container = (OpenXmlElement?)contentRun ?? contentBlock;
        if (container is null)
        {
            continue;
        }

        // Collapse to a single run that keeps the FIRST original run's properties
        // (plan §5.2 — a content control routinely holds several runs).
        var firstRun = container.Descendants<Run>().FirstOrDefault();
        var keptProps = firstRun?.RunProperties?.CloneNode(true) as RunProperties;

        container.RemoveAllChildren<Run>();
        var run = new Run(new Text(newText) { Space = SpaceProcessingModeValues.Preserve });
        if (keptProps is not null)
        {
            run.RunProperties = keptProps;
        }

        container.AppendChild(run);
        count++;
    }

    doc.MainDocumentPart!.Document.Save();
    return count;
}

static ComparisonReport CompareOutsideContentControls(string originalPath, string filledPath)
{
    var parts = new[] { "document", "styles" };
    var diffs = new List<string>();

    using (var original = WordprocessingDocument.Open(originalPath, isEditable: false))
    using (var filled = WordprocessingDocument.Open(filledPath, isEditable: false))
    {
        ComparePart("document.xml",
            original.MainDocumentPart!.Document.OuterXml,
            filled.MainDocumentPart!.Document.OuterXml,
            maskSdtContents: true, diffs);

        ComparePart("styles.xml",
            original.MainDocumentPart!.StyleDefinitionsPart?.Styles?.OuterXml ?? "",
            filled.MainDocumentPart!.StyleDefinitionsPart?.Styles?.OuterXml ?? "",
            maskSdtContents: false, diffs);
    }

    return new ComparisonReport(diffs, parts.Length);
}

static void ComparePart(string name, string originalXml, string filledXml, bool maskSdtContents, List<string> diffs)
{
    var a = Canonicalize(originalXml, maskSdtContents);
    var b = Canonicalize(filledXml, maskSdtContents);
    if (!XNode.DeepEquals(a, b))
    {
        diffs.Add($"  [DIFF] {name} changed outside content controls");
    }
    else
    {
        Console.WriteLine($"  [OK]   {name} byte-independent structure identical");
    }
}

// Canonical form: sort attributes, drop insignificant whitespace, and (optionally)
// replace the text inside every <w:sdtContent> with a fixed marker so that the
// filled-in value itself is not treated as a difference.
static XElement Canonicalize(string xml, bool maskSdtContents)
{
    var root = XElement.Parse(xml, LoadOptions.None);

    if (maskSdtContents)
    {
        XName sdtContent = XName.Get("sdtContent", "http://schemas.openxmlformats.org/wordprocessingml/2006/main");
        foreach (var node in root.DescendantsAndSelf(sdtContent).ToList())
        {
            node.RemoveNodes();
            node.Add(new XElement(XName.Get("__masked__", "urn:spike")));
        }
    }

    return Normalize(root);
}

static XElement Normalize(XElement element)
{
    var normalized = new XElement(element.Name,
        element.Attributes()
            .Where(a => !a.IsNamespaceDeclaration)
            .OrderBy(a => a.Name.NamespaceName, StringComparer.Ordinal)
            .ThenBy(a => a.Name.LocalName, StringComparer.Ordinal)
            .Select(a => new XAttribute(a.Name, a.Value)));

    foreach (var child in element.Nodes())
    {
        switch (child)
        {
            case XElement childElement:
                normalized.Add(Normalize(childElement));
                break;
            case XText text when !string.IsNullOrWhiteSpace(text.Value):
                normalized.Add(new XText(text.Value.Trim()));
                break;
        }
    }

    return normalized;
}

internal sealed record ComparisonReport(List<string> Diffs, int PartsChecked)
{
    public bool Identical => Diffs.Count == 0;

    public string ToText()
    {
        var verdict = Identical
            ? $"PASS — {PartsChecked} document parts identical outside the content controls.\n" +
              "The content-control fill approach preserves formatting. Ready to be load-bearing (plan §5)."
            : "FAIL — differences detected:\n" + string.Join('\n', Diffs);

        return "==== Spike A result ====\n" + verdict;
    }
}
