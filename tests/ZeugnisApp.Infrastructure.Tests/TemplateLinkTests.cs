using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ZeugnisApp.Application.Abstractions;
using ZeugnisApp.Application.Reporting;
using ZeugnisApp.Domain.Entities;
using ZeugnisApp.Domain.Enums;
using ZeugnisApp.Infrastructure.Services;
using ZeugnisApp.Reporting;
using ZeugnisApp.Reporting.Templates;

namespace ZeugnisApp.Infrastructure.Tests;

public sealed class TemplateLinkTests : IDisposable
{
    private readonly string _work = Path.Combine(Path.GetTempPath(), "zeugnis-link", Guid.NewGuid().ToString("N"));
    private readonly List<string> _appDataFiles = [];

    public TemplateLinkTests() => Directory.CreateDirectory(_work);

    public void Dispose()
    {
        try { Directory.Delete(_work, true); } catch (IOException) { }
        foreach (var f in _appDataFiles)
        {
            try { File.Delete(f); } catch (IOException) { }
        }
    }

    [Fact]
    public async Task LinkTemplate_copies_the_file_persists_the_mapping_and_generation_then_works()
    {
        await using var db = new SqliteTestDatabase();
        await new DatabaseInitializer(db.ContextFactory).InitializeAsync();
        var formulare = new FormulareService(db.ContextFactory, db.Clock);

        // A teacher's uploaded template lives outside the app folder.
        var k3 = (await formulare.GetAllAsync()).First(f => f.Anzeigename == "Klasse 3 – 1. Halbjahr");
        var formular = await formulare.GetAsync(k3.Id);
        var uploaded = Path.Combine(_work, "meine-vorlage.docx");
        var mapping = PlaceholderTemplateBuilder.Build(uploaded, formular!);

        var stored = await formulare.LinkTemplateAsync(k3.Id, uploaded, mapping.ToJson());
        _appDataFiles.Add(stored);

        Assert.True(File.Exists(stored));
        Assert.NotEqual(Path.GetFullPath(uploaded), Path.GetFullPath(stored)); // copied, not referenced

        var reloaded = await formulare.GetAsync(k3.Id);
        Assert.Equal(stored, reloaded!.WordFilePath);
        Assert.Contains("kind.name", reloaded.FieldMappingJson!, StringComparison.OrdinalIgnoreCase);

        // The linked template really generates.
        var kind = new Kind { Vorname = "Mia", Nachname = "Klein", Geschlecht = Geschlecht.Weiblich };
        var klasse = new Klasse { Name = "3a", Jahrgang = 3 };
        var zeugnis = new Zeugnis { Kind = kind, Formular = reloaded, Schuljahr = "2026/27", Halbjahr = Halbjahr.Erstes };
        var request = new ReportRenderRequest(zeugnis, kind, klasse, reloaded, new SchoolSettings { Name = "GS Test" });

        var target = Path.Combine(_work, "out.docx");
        var result = await new OpenXmlReportGenerationEngine().GenerateAsync(request, target);

        Assert.True(result.Success, result.ErrorMessage);
        using var doc = WordprocessingDocument.Open(target, isEditable: false);
        var text = string.Concat(doc.MainDocumentPart!.Document.Descendants<Text>().Select(t => t.Text));
        Assert.Contains("Mia Klein", text, StringComparison.Ordinal);
    }
}
