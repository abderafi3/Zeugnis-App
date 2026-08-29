using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ZeugnisApp.Application.Reporting;
using ZeugnisApp.Domain.Entities;

namespace ZeugnisApp.Reporting.Tests;

/// <summary>
/// Builds a synthetic, content-controlled <c>.docx</c> for a template (the school's real templates
/// are not in the repo). Deliberately includes: a styled heading, a bordered grade table, block and
/// inline content controls each holding <em>two</em> runs (to exercise the run-collapse), a header
/// with a content control, a footer without one, and an orphan media part — so the canonical-XML
/// regression (plan §5.4) has real structure to protect.
/// </summary>
internal static class TemplateFixture
{
    private static int _sdtId = 1000;

    public static void Build(string path, Formular formular, FieldMapping mapping)
    {
        _sdtId = 1000;
        using var doc = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document);
        var main = doc.AddMainDocumentPart();

        var stylePart = main.AddNewPart<StyleDefinitionsPart>();
        stylePart.Styles = new Styles(
            new Style(
                new StyleName { Val = "Heading1" },
                new StyleRunProperties(new Bold(), new FontSize { Val = "32" }))
            {
                Type = StyleValues.Paragraph, StyleId = "Heading1",
            });
        stylePart.Styles.Save();

        // Orphan media part — must survive the round-trip untouched.
        var image = main.AddImagePart(ImagePartType.Png);
        using (var png = new MemoryStream(OnePixelPng))
        {
            image.FeedData(png);
        }

        var headerPart = main.AddNewPart<HeaderPart>();
        headerPart.Header = new Header(
            new Paragraph(new Run(new Text("Grundschule – Zeugnis") { Space = SpaceProcessingModeValues.Preserve })),
            new Paragraph(InlineSdt(Tag(mapping, ValueKeys.SchuleName), "[", "Schulname]")));
        headerPart.Header.Save();

        var footerPart = main.AddNewPart<FooterPart>();
        footerPart.Footer = new Footer(new Paragraph(new Run(new Text("Seite 1"))));
        footerPart.Footer.Save();

        var body = new Body();

        body.AppendChild(new Paragraph(
            new ParagraphProperties(new ParagraphStyleId { Val = "Heading1" }),
            new Run(new Text(formular.Ueberschrift ?? "Zeugnis"))));

        body.AppendChild(Line("Name: ", InlineSdt(Tag(mapping, ValueKeys.KindName), "[Name ", "eintragen]")));
        body.AppendChild(Line("Klasse: ", InlineSdt(Tag(mapping, ValueKeys.KlasseName), "[", "Klasse]")));
        body.AppendChild(Line("Schuljahr: ", InlineSdt(Tag(mapping, ValueKeys.SchuleSchuljahr), "[", "Jahr]")));

        if (formular.HatNotenblock)
        {
            body.AppendChild(GradeTable(formular, mapping));
        }

        foreach (var feld in formular.Freitextfelder)
        {
            body.AppendChild(new Paragraph(new Run(new RunProperties(new Bold()), new Text(feld.Name))));
            body.AppendChild(BlockSdt(Tag(mapping, ValueKeys.Freitext(feld.Name)), "[Freitext ", "hier]"));
        }

        if (formular.HatBegruendeteEmpfehlung)
        {
            body.AppendChild(Line("Empfehlung: ", InlineSdt(Tag(mapping, ValueKeys.ZeugnisEmpfehlung), "[", "Schulform]")));
            body.AppendChild(new Paragraph(new Run(new Text("Begründung:"))));
            body.AppendChild(BlockSdt(Tag(mapping, ValueKeys.ZeugnisEmpfehlungBegruendung), "[Begründung ", "hier]"));
        }

        if (formular.HatVersetzungsvermerk)
        {
            body.AppendChild(Line("Vermerk: ", InlineSdt(Tag(mapping, ValueKeys.ZeugnisVersetzungsvermerk), "[", "Vermerk]")));
        }

        body.AppendChild(Line("Fehltage gesamt: ", InlineSdt(Tag(mapping, ValueKeys.ZeugnisFehlstundenGesamt), "[", "0]")));
        body.AppendChild(Line("davon unentschuldigt: ", InlineSdt(Tag(mapping, ValueKeys.ZeugnisFehlstundenUnentschuldigt), "[", "0]")));

        body.AppendChild(new SectionProperties(
            new HeaderReference { Type = HeaderFooterValues.Default, Id = main.GetIdOfPart(headerPart) },
            new FooterReference { Type = HeaderFooterValues.Default, Id = main.GetIdOfPart(footerPart) }));

        main.Document = new Document(body);
        main.Document.Save();
    }

    private static string Tag(FieldMapping mapping, string valueKey)
        => mapping.TagFor(valueKey) ?? throw new InvalidOperationException($"Kein Tag für {valueKey}");

    private static Paragraph Line(string label, OpenXmlElement sdt)
        => new(new Run(new Text(label) { Space = SpaceProcessingModeValues.Preserve }), sdt);

    /// <summary>Inline SDT whose content is split into two runs; the first carries the formatting.</summary>
    private static SdtRun InlineSdt(string tag, string part1, string part2) => new(
        new SdtProperties(new SdtId { Val = _sdtId++ }, new Tag { Val = tag }, new SdtContentText()),
        new SdtContentRun(
            new Run(new RunProperties(new Bold(), new Color { Val = "1F4E79" }), new Text(part1) { Space = SpaceProcessingModeValues.Preserve }),
            new Run(new Text(part2) { Space = SpaceProcessingModeValues.Preserve })));

    /// <summary>Block SDT whose paragraph holds two runs; the first carries the formatting.</summary>
    private static SdtBlock BlockSdt(string tag, string part1, string part2) => new(
        new SdtProperties(new SdtId { Val = _sdtId++ }, new Tag { Val = tag }),
        new SdtContentBlock(new Paragraph(
            new ParagraphProperties(new SpacingBetweenLines { After = "120" }),
            new Run(new RunProperties(new Italic()), new Text(part1) { Space = SpaceProcessingModeValues.Preserve }),
            new Run(new Text(part2) { Space = SpaceProcessingModeValues.Preserve }))));

    private static Table GradeTable(Formular formular, FieldMapping mapping)
    {
        var table = new Table(new TableProperties(new TableBorders(
            new TopBorder { Val = BorderValues.Single, Size = 4 },
            new BottomBorder { Val = BorderValues.Single, Size = 4 },
            new LeftBorder { Val = BorderValues.Single, Size = 4 },
            new RightBorder { Val = BorderValues.Single, Size = 4 },
            new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4 },
            new InsideVerticalBorder { Val = BorderValues.Single, Size = 4 })));

        foreach (var fach in formular.Faecher)
        {
            table.AppendChild(new TableRow(
                new TableCell(new Paragraph(new Run(new Text(fach)))),
                new TableCell(new Paragraph(InlineSdt(Tag(mapping, ValueKeys.Note(fach)), "[", "Note]")))));
        }

        return table;
    }

    // 1x1 transparent PNG.
    private static readonly byte[] OnePixelPng = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==");
}
