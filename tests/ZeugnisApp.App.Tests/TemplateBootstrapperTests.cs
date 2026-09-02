using System.IO;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ZeugnisApp.App.Services;
using ZeugnisApp.Application.Abstractions;
using ZeugnisApp.Application.Reporting;
using ZeugnisApp.Domain.Entities;
using ZeugnisApp.Domain.Enums;
using ZeugnisApp.Infrastructure;
using ZeugnisApp.Infrastructure.Persistence;
using ZeugnisApp.Reporting;
using ZeugnisApp.Reporting.Ooxml;

namespace ZeugnisApp.App.Tests;

public sealed class TemplateBootstrapperTests : IAsyncLifetime
{
    static TemplateBootstrapperTests() => SQLitePCL.Batteries_V2.Init();

    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"tb-{Guid.NewGuid():N}.db");
    private ServiceProvider _sp = null!;
    private readonly List<string> _writtenTemplates = [];

    public async Task InitializeAsync()
    {
        var services = new ServiceCollection();
        services.AddZeugnisInfrastructure($"Data Source={_dbPath}");
        _sp = services.BuildServiceProvider();
        await _sp.GetRequiredService<DatabaseInitializer>().InitializeAsync();
    }

    public async Task DisposeAsync()
    {
        foreach (var f in _writtenTemplates.Where(File.Exists))
        {
            try { File.Delete(f); } catch (IOException) { /* best effort */ }
        }

        await _sp.DisposeAsync();
        try { await using var db = _sp.GetService<IDbContextFactory<ZeugnisDbContext>>()!.CreateDbContext(); await db.Database.EnsureDeletedAsync(); }
        catch { /* best effort */ }
        foreach (var f in Directory.GetFiles(Path.GetTempPath(), Path.GetFileName(_dbPath) + "*"))
        {
            try { File.Delete(f); } catch (IOException) { /* best effort */ }
        }
    }

    [Fact]
    public async Task Links_every_standard_form_to_its_bundled_tagged_docx()
    {
        var formulare = _sp.GetRequiredService<IFormulareService>();
        await new TemplateBootstrapper(formulare).EnsureTemplatesAsync();

        foreach (var item in await formulare.GetAllAsync())
        {
            var f = await formulare.GetAsync(item.Id);
            Assert.NotNull(f!.WordFilePath);
            Assert.True(File.Exists(f.WordFilePath), f.Anzeigename + ": " + f.WordFilePath);
            _writtenTemplates.Add(f.WordFilePath!);

            var mapping = FieldMapping.FromJson(f.FieldMappingJson);
            Assert.Equal("StudentName", mapping.TagFor(ValueKeys.KindName));
            Assert.Equal("Freitext_Bemerkungen", mapping.TagFor(ValueKeys.ZeugnisBemerkungen));

            if (f.HatNotenblock)
            {
                Assert.Equal("Note_Deutsch", mapping.TagFor(ValueKeys.Note("Deutsch")));
                Assert.Equal("Note_Deutsch_Sprachgebrauch", mapping.TagFor(ValueKeys.Note("Deutsch Sprachgebrauch")));
            }

            // the bundled file really is one of the tagged forms (has content controls)
            using var doc = WordprocessingDocument.Open(f.WordFilePath!, false);
            Assert.NotEmpty(doc.MainDocumentPart!.Document.Descendants<SdtElement>());
        }
    }

    [Fact]
    public async Task Does_not_re_link_a_form_that_already_has_a_template()
    {
        var formulare = _sp.GetRequiredService<IFormulareService>();
        var bootstrapper = new TemplateBootstrapper(formulare);

        await bootstrapper.EnsureTemplatesAsync();
        var first = await formulare.GetAllAsync();
        var pathsBefore = new Dictionary<Guid, string?>();
        foreach (var i in first)
        {
            var f = await formulare.GetAsync(i.Id);
            pathsBefore[i.Id] = f!.WordFilePath;
            if (f.WordFilePath is not null)
            {
                _writtenTemplates.Add(f.WordFilePath);
            }
        }

        await bootstrapper.EnsureTemplatesAsync(); // second pass

        foreach (var i in await formulare.GetAllAsync())
        {
            var f = await formulare.GetAsync(i.Id);
            Assert.Equal(pathsBefore[i.Id], f!.WordFilePath);
        }
    }

    [Fact]
    public async Task A_custom_form_falls_back_to_a_generated_placeholder()
    {
        var formulare = _sp.GetRequiredService<IFormulareService>();
        var custom = await formulare.SaveAsync(new Formular
        {
            Anzeigename = "Eigenes Formular XYZ",
            Jahrgang = 2,
            GiltFuerHalbjahr = Halbjahr.Erstes,
            Freitextfelder = [new() { Name = "Deutsch" }],
        });

        await new TemplateBootstrapper(formulare).EnsureTemplatesAsync();

        var reloaded = await formulare.GetAsync(custom.Id);
        Assert.NotNull(reloaded!.WordFilePath);
        Assert.True(File.Exists(reloaded.WordFilePath));
        _writtenTemplates.Add(reloaded.WordFilePath!);
        Assert.Contains("Eigenes Formular XYZ", Path.GetFileName(reloaded.WordFilePath!), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Generated_report_from_a_bundled_template_fills_and_preserves_structure()
    {
        var formulare = _sp.GetRequiredService<IFormulareService>();
        await new TemplateBootstrapper(formulare).EnsureTemplatesAsync();

        var k3 = (await formulare.GetAllAsync()).First(f => f.Anzeigename == "Klasse 3 – 1. Halbjahr");
        var formular = await formulare.GetAsync(k3.Id);
        _writtenTemplates.Add(formular!.WordFilePath!);

        var schule = new SchoolSettings { Name = "Grundschule Wehringhausen", Zeugnisausgabedatum = new DateOnly(2027, 1, 29) };
        var klasse = new Klasse { Name = "F1", Jahrgang = 3, Zeugnisdatum = new DateOnly(2027, 1, 29) };
        var kind = new Kind { Vorname = "Erika", Nachname = "Mustermann", Geschlecht = Geschlecht.Weiblich, Geburtsdatum = new DateOnly(2018, 6, 14) };
        var zeugnis = new Zeugnis
        {
            Schuljahr = "2026/27", Halbjahr = Halbjahr.Erstes,
            Bemerkungen = "Erika hat an der Theater-AG teilgenommen.",
            FehlstundenGesamt = 4, FehlstundenUnentschuldigt = 1,
            Noten = [.. formular.Faecher.Select(fa => new NoteEintrag { Fach = fa, Note = "gut" })],
            Freitexte = [.. formular.Freitextfelder.Select(fe => new FreitextEintrag { FeldName = fe.Name, Text = $"Bericht {fe.Name}.\nZweite Zeile." })],
        };

        var target = Path.Combine(Path.GetTempPath(), $"k3-gen-{Guid.NewGuid():N}.docx");
        var result = await new OpenXmlReportGenerationEngine()
            .GenerateAsync(new ReportRenderRequest(zeugnis, kind, klasse, formular, schule), target);

        Assert.True(result.Success, result.ErrorMessage);
        try
        {
            using (var doc = WordprocessingDocument.Open(target, false))
            {
                var all = string.Concat(doc.MainDocumentPart!.Document.Descendants<Text>().Select(t => t.Text));
                Assert.Contains("Erika Mustermann", all, StringComparison.Ordinal);
                Assert.Contains("14.06.2018", all, StringComparison.Ordinal);
                Assert.Contains("Erika hat an der Theater-AG teilgenommen.", all, StringComparison.Ordinal);
                Assert.Contains("Bericht Arbeits- und Sozialverhalten.", all, StringComparison.Ordinal);
            }

            var cmp = OoxmlCanonicalizer.CompareStructure(formular.WordFilePath!, target);
            Assert.True(cmp.Identical, "Struktur verändert:\n" + string.Join("\n", cmp.Differences));
        }
        finally
        {
            File.Delete(target);
        }
    }
}
