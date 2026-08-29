using ZeugnisApp.Application.Abstractions;
using ZeugnisApp.Application.Formulare;
using ZeugnisApp.Application.Reporting;
using ZeugnisApp.Reporting.Ooxml;
using ZeugnisApp.Reporting.Templates;

namespace ZeugnisApp.Reporting.Tests;

public sealed class TemplateInspectorTests : IDisposable
{
    private readonly string _work = Path.Combine(Path.GetTempPath(), "zeugnis-inspector", Guid.NewGuid().ToString("N"));

    public TemplateInspectorTests() => Directory.CreateDirectory(_work);
    public void Dispose() { try { Directory.Delete(_work, true); } catch (IOException) { } }

    [Fact]
    public void Reads_every_content_control_tag_from_body_and_header()
    {
        var formular = StandardFormulare.All().First(f => f.HatNotenblock);
        var path = Path.Combine(_work, "t.docx");
        var mapping = PlaceholderTemplateBuilder.Build(path, formular);

        var controls = new TemplateInspector().ReadContentControls(path);

        var tags = controls.Select(c => c.Tag).ToList();
        Assert.Contains(mapping.TagFor(ValueKeys.KindName), tags);
        Assert.Contains(mapping.TagFor(ValueKeys.SchuleName), tags);   // lives in the header
        Assert.Contains(mapping.TagFor(ValueKeys.Note("Deutsch")), tags);
        Assert.Contains("Kopfzeile", controls.Select(c => c.Location));
        Assert.All(controls, c => Assert.False(string.IsNullOrWhiteSpace(c.Tag)));
    }

    [Fact]
    public void Throws_InvalidTemplateException_for_a_file_that_is_not_a_docx()
    {
        var path = Path.Combine(_work, "notreally.docx");
        File.WriteAllText(path, "this is not a word document");

        Assert.Throws<InvalidTemplateException>(() => new TemplateInspector().ReadContentControls(path));
    }
}
