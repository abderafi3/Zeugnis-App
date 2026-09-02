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

/// <summary>
/// Closes the Sprint 9 loop: a DB-backed Zeugnis, its placeholder template, the real value resolver
/// and the real engine produce an opened, correct <c>.docx</c> (plan §Sprint 9 DoD).
/// </summary>
public sealed class ReportGenerationEndToEndTests : IDisposable
{
    private readonly string _work = Path.Combine(Path.GetTempPath(), "zeugnis-e2e", Guid.NewGuid().ToString("N"));

    public ReportGenerationEndToEndTests() => Directory.CreateDirectory(_work);
    public void Dispose() { try { Directory.Delete(_work, true); } catch (IOException) { } }

    [Fact]
    public async Task Full_flow_from_database_to_a_filled_docx()
    {
        await using var db = new SqliteTestDatabase();
        await new DatabaseInitializer(db.ContextFactory).InitializeAsync();

        Guid kindId;
        await using (var ctx = db.NewContext())
        {
            var klasse = new Klasse { Name = "3a", Jahrgang = 3, Klassenleitung = "Herr Meyer" };
            var kind = new Kind
            {
                Vorname = "Erika", Nachname = "Mustermann", Geschlecht = Geschlecht.Weiblich,
                Geburtsdatum = new DateOnly(2018, 6, 14), Klasse = klasse,
            };
            // SchoolSettings is seeded by DatabaseInitializer (school identity) — no need to add one here.
            ctx.AddRange(klasse, kind);
            await ctx.SaveChangesAsync();
            kindId = kind.Id;
        }

        var formulare = new FormulareService(db.ContextFactory, db.Clock);
        var zeugnisse = new ZeugnisService(db.ContextFactory);

        // Bootstrap the placeholder template for the Klasse-3 HJ1 form.
        var k3 = (await formulare.GetAllAsync()).First(f => f.Anzeigename == "Klasse 3 – 1. Halbjahr");
        var formular = await formulare.GetAsync(k3.Id);
        var templatePath = Path.Combine(_work, "k3.docx");
        var mapping = PlaceholderTemplateBuilder.Build(templatePath, formular!);
        formular!.WordFilePath = templatePath;
        formular.FieldMappingJson = mapping.ToJson();
        await formulare.SaveAsync(formular);

        // Create + fill the report card.
        var created = (await zeugnisse.GetOrCreateAsync(kindId, "2026/27", Halbjahr.Erstes)).Zeugnis!;
        created.Freitexte.Add(new FreitextEintrag { FeldName = "Arbeits- und Sozialverhalten", Text = "Erika arbeitet konzentriert." });
        created.Noten.Add(new NoteEintrag { Fach = "Deutsch", Note = "sehr gut" });
        await zeugnisse.SaveAsync(created);

        var full = await zeugnisse.GetWithContextAsync(created.Id);
        var request = new ReportRenderRequest(full!, full!.Kind!, full.Kind!.Klasse!, full.Formular!,
            await new SchoolSettingsService(db.NewContext()).GetAsync());

        var target = Path.Combine(_work, "erzeugt.docx");
        var result = await new OpenXmlReportGenerationEngine().GenerateAsync(request, target);

        Assert.True(result.Success, result.ErrorMessage);
        Assert.True(File.Exists(target));

        using var doc = WordprocessingDocument.Open(target, isEditable: false);
        var allText = string.Concat(doc.MainDocumentPart!.Document.Descendants<Text>().Select(t => t.Text));
        Assert.Contains("Erika Mustermann", allText, StringComparison.Ordinal);
        Assert.Contains("Erika arbeitet konzentriert.", allText, StringComparison.Ordinal);
        Assert.Contains("sehr gut", allText, StringComparison.Ordinal);

        // Untouched structure.
        Assert.True(Reporting.Ooxml.OoxmlCanonicalizer.CompareStructure(templatePath, target).Identical);
    }
}
