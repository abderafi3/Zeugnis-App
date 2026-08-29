using System.IO;
using Serilog;
using ZeugnisApp.Application.Abstractions;
using ZeugnisApp.Infrastructure;
using ZeugnisApp.Reporting.Templates;

namespace ZeugnisApp.App.Services;

/// <summary>
/// Ensures every Formular has a usable Word template so "Zeugnis erzeugen" works out of the box.
/// Generates a plain content-controlled placeholder (<see cref="PlaceholderTemplateBuilder"/>) for
/// any Formular whose <c>WordFilePath</c> is missing. The Sprint 10 upload replaces these with the
/// school's official <c>.docx</c> files.
/// </summary>
public sealed class TemplateBootstrapper(IFormulareService formulare)
{
    public async Task EnsureTemplatesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var item in await formulare.GetAllAsync(cancellationToken).ConfigureAwait(false))
        {
            var formular = await formulare.GetAsync(item.Id, cancellationToken).ConfigureAwait(false);
            if (formular is null)
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(formular.WordFilePath) && File.Exists(formular.WordFilePath))
            {
                continue;
            }

            var path = Path.Combine(ZeugnisAppPaths.TemplatesDirectory, $"{SafeName(formular.Anzeigename)}.docx");
            try
            {
                var mapping = PlaceholderTemplateBuilder.Build(path, formular);
                formular.WordFilePath = path;
                formular.FieldMappingJson = mapping.ToJson();
                await formulare.SaveAsync(formular, cancellationToken).ConfigureAwait(false);
                Log.Information("Platzhalter-Vorlage für {Formular} erzeugt: {Path}", formular.Anzeigename, path);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Platzhalter-Vorlage für {Formular} konnte nicht erzeugt werden", formular.Anzeigename);
            }
        }
    }

    private static string SafeName(string value)
    {
        var cleaned = string.Concat(value.Select(c => char.IsLetterOrDigit(c) || c is ' ' or '-' or '_' ? c : '_')).Trim();
        return string.IsNullOrEmpty(cleaned) ? "Formular" : cleaned;
    }
}
