using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ZeugnisApp.Application.Abstractions;
using ZeugnisApp.Application.Formulare;
using ZeugnisApp.Application.Reporting;
using ZeugnisApp.Domain.Entities;
using ZeugnisApp.Domain.Enums;
using ZeugnisApp.Reporting;
using ZeugnisApp.Reporting.Ooxml;
using ZeugnisApp.Reporting.Templates;

namespace ZeugnisApp.Reporting.Tests;

/// <summary>
/// The Sprint 6 regression suite — the MVP release gate (plan §5.4, §12). For every standard
/// template: generate from fixed sample data, then assert (a) the values landed in the right content
/// controls and (b) everything outside the content controls is structurally identical to the
/// pristine template.
/// </summary>
public sealed class ReportGenerationRegressionTests : IDisposable
{
    private readonly string _work = Path.Combine(Path.GetTempPath(), "zeugnis-reporting-tests", Guid.NewGuid().ToString("N"));
    private readonly OpenXmlReportGenerationEngine _engine = new();

    public ReportGenerationRegressionTests() => Directory.CreateDirectory(_work);

    public void Dispose()
    {
        try { Directory.Delete(_work, recursive: true); } catch (IOException) { /* best effort */ }
    }

    public static IEnumerable<object[]> AlleFormulare =>
        StandardFormulare.All().Select(f => new object[] { f.Anzeigename });

    [Theory]
    [MemberData(nameof(AlleFormulare))]
    public async Task Template_fill_changes_nothing_outside_the_content_controls(string anzeigename)
    {
        var formular = StandardFormulare.All().First(f => f.Anzeigename == anzeigename);
        var mapping = FieldMapping.Default(formular);
        var pristine = Path.Combine(_work, $"{Sanitize(anzeigename)}.pristine.docx");
        var filled = Path.Combine(_work, $"{Sanitize(anzeigename)}.filled.docx");

        PlaceholderTemplateBuilder.Build(pristine, formular, mapping);
        formular.WordFilePath = pristine;
        formular.FieldMappingJson = mapping.ToJson();

        var request = SampleRequest(formular);
        var result = await _engine.GenerateAsync(request, filled);

        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal(filled, result.FilePath);

        var comparison = OoxmlCanonicalizer.CompareStructure(pristine, filled);
        Assert.True(comparison.Identical,
            "Struktur außerhalb der Inhaltssteuerelemente verändert:\n" + string.Join("\n", comparison.Differences));
    }

    [Theory]
    [MemberData(nameof(AlleFormulare))]
    public async Task Template_fill_writes_the_expected_values_into_the_tags(string anzeigename)
    {
        var formular = StandardFormulare.All().First(f => f.Anzeigename == anzeigename);
        var mapping = FieldMapping.Default(formular);
        var pristine = Path.Combine(_work, $"{Sanitize(anzeigename)}.v.pristine.docx");
        var filled = Path.Combine(_work, $"{Sanitize(anzeigename)}.v.filled.docx");

        PlaceholderTemplateBuilder.Build(pristine, formular, mapping);
        formular.WordFilePath = pristine;
        formular.FieldMappingJson = mapping.ToJson();

        var request = SampleRequest(formular);
        var values = ReportValues.Build(request);
        Assert.True((await _engine.GenerateAsync(request, filled)).Success);

        using var doc = WordprocessingDocument.Open(filled, isEditable: false);

        AssertTag(doc, mapping.TagFor(ValueKeys.KindName)!, request.Kind.AnzeigeName);
        AssertTag(doc, mapping.TagFor(ValueKeys.KlasseName)!, request.Klasse.Name);
        AssertTag(doc, mapping.TagFor(ValueKeys.SchuleName)!, request.Schule.Name); // in the header

        foreach (var feld in formular.Freitextfelder)
        {
            AssertTag(doc, mapping.TagFor(ValueKeys.Freitext(feld.Name))!, values[ValueKeys.Freitext(feld.Name)]);
        }

        foreach (var fach in formular.Faecher)
        {
            AssertTag(doc, mapping.TagFor(ValueKeys.Note(fach))!, values[ValueKeys.Note(fach)]);
        }
    }

    [Fact]
    public async Task Multi_run_content_control_is_collapsed_to_one_run_keeping_its_formatting()
    {
        var formular = StandardFormulare.All().First(f => f.HatNotenblock);
        var mapping = FieldMapping.Default(formular);
        var pristine = Path.Combine(_work, "collapse.pristine.docx");
        var filled = Path.Combine(_work, "collapse.filled.docx");

        PlaceholderTemplateBuilder.Build(pristine, formular, mapping);
        formular.WordFilePath = pristine;
        formular.FieldMappingJson = mapping.ToJson();

        Assert.True((await _engine.GenerateAsync(SampleRequest(formular), filled)).Success);

        using var doc = WordprocessingDocument.Open(filled, isEditable: false);
        var sdt = FindSdt(doc.MainDocumentPart!.Document, mapping.TagFor(ValueKeys.KindName)!);
        var runs = sdt!.Descendants<Run>().ToList();

        Assert.Single(runs);
        Assert.NotNull(runs[0].RunProperties);
        Assert.NotNull(runs[0].RunProperties!.GetFirstChild<Bold>());
    }

    [Fact]
    public async Task Generation_fails_gracefully_when_no_template_is_linked()
    {
        var formular = StandardFormulare.All().First();
        formular.WordFilePath = null;

        var result = await _engine.GenerateAsync(SampleRequest(formular), Path.Combine(_work, "none.docx"));

        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        Assert.False(File.Exists(Path.Combine(_work, "none.docx")));
    }

    [Fact]
    public void CheckOverflow_reports_fields_over_their_character_budget()
    {
        var formular = StandardFormulare.All().First(f => !f.HatNotenblock);
        formular.Freitextfelder[0].MaxZeichen = 20;

        var request = SampleRequest(formular);
        request.Zeugnis.Freitexte.First(f => f.FeldName == formular.Freitextfelder[0].Name).Text =
            new string('x', 50);

        var warnings = _engine.CheckOverflow(request);

        var warning = Assert.Single(warnings);
        Assert.Equal(formular.Freitextfelder[0].Name, warning.FeldName);
        Assert.Equal(50, warning.IstZeichen);
        Assert.Equal(20, warning.MaxZeichen);
        Assert.Equal(30, warning.UeberschussZeichen);
    }

    // ---------------------------------------------------------------------

    private static ReportRenderRequest SampleRequest(Formular formular)
    {
        var schule = new SchoolSettings
        {
            Name = "Grundschule Wehringhausen",
            Zusatzbezeichnung = "Städt. Gemeinschaftsgrundschule",
            Adresse = "Musterstraße 1, 58089 Hagen",
            Schulleitung = "Frau Dr. Schulz",
            Zeugnisausgabedatum = new DateOnly(2027, 1, 29),
        };

        var klasse = new Klasse
        {
            Name = "3a",
            Jahrgang = formular.Jahrgang,
            Klassenleitung = "Herr Meyer",
            Zeugnisdatum = new DateOnly(2027, 1, 29),
        };

        var kind = new Kind
        {
            Vorname = "Erika",
            Nachname = "Mustermann",
            Geschlecht = Geschlecht.Weiblich,
            Geburtsdatum = new DateOnly(2018, 6, 14),
            KlasseId = klasse.Id,
        };

        var zeugnis = new Zeugnis
        {
            KindId = kind.Id,
            FormularId = formular.Id,
            Schuljahr = "2026/27",
            Halbjahr = formular.GiltFuerHalbjahr ?? Halbjahr.Erstes,
            Empfehlung = "Gymnasium",
            EmpfehlungBegruendung = "Erika arbeitet selbstständig und zeigt durchgehend sehr gute Leistungen.",
            VersetzungsvermerkText = "Erika wird in die Klasse 4 versetzt.",
            Bemerkungen = "Erika hat an der Theater-AG teilgenommen.",
            FehlstundenGesamt = 4,
            FehlstundenUnentschuldigt = 1,
            Noten = [.. formular.Faecher.Select(fach => new NoteEintrag { Fach = fach, Note = "sehr gut" })],
            Freitexte = [.. formular.Freitextfelder.Select(f => new FreitextEintrag
            {
                FeldName = f.Name,
                Text = $"Erika zeigt im Bereich {f.Name} großes Engagement.",
            })],
        };

        return new ReportRenderRequest(zeugnis, kind, klasse, formular, schule);
    }

    private static void AssertTag(WordprocessingDocument doc, string tag, string expected)
    {
        var text = ReadSdtText(doc, tag);
        Assert.True(text is not null, $"Inhaltssteuerelement mit Tag '{tag}' nicht gefunden.");
        Assert.Equal(expected, text);
    }

    private static string? ReadSdtText(WordprocessingDocument doc, string tag)
    {
        var roots = new List<DocumentFormat.OpenXml.OpenXmlElement> { doc.MainDocumentPart!.Document };
        roots.AddRange(doc.MainDocumentPart!.HeaderParts.Select(h => (DocumentFormat.OpenXml.OpenXmlElement)h.Header));
        roots.AddRange(doc.MainDocumentPart!.FooterParts.Select(f => (DocumentFormat.OpenXml.OpenXmlElement)f.Footer));

        foreach (var root in roots)
        {
            var sdt = FindSdt(root, tag);
            if (sdt is not null)
            {
                var content = (DocumentFormat.OpenXml.OpenXmlElement?)sdt.Descendants<SdtContentRun>().FirstOrDefault()
                              ?? sdt.Descendants<SdtContentBlock>().FirstOrDefault();
                return content is null ? null : string.Concat(content.Descendants<Text>().Select(t => t.Text));
            }
        }

        return null;
    }

    private static SdtElement? FindSdt(DocumentFormat.OpenXml.OpenXmlElement root, string tag)
        => root.Descendants<SdtElement>()
            .FirstOrDefault(s => s.SdtProperties?.GetFirstChild<Tag>()?.Val?.Value == tag);

    private static string Sanitize(string name)
        => string.Concat(name.Where(c => char.IsLetterOrDigit(c) || c is '-' or '_'));
}
