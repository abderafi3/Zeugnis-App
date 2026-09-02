using System.IO;
using Serilog;
using ZeugnisApp.Application.Abstractions;
using ZeugnisApp.Domain.Entities;
using ZeugnisApp.Infrastructure;
using ZeugnisApp.Reporting.Templates;

namespace ZeugnisApp.App.Services;

/// <summary>
/// Ensures every <see cref="Formular"/> has a usable Word template so "Zeugnis erzeugen" works out
/// of the box:
/// <list type="bullet">
///   <item>a standard form gets Grundschule Wehringhausen's real, tagged <c>.docx</c> (bundled in
///   <see cref="AmtlicheVorlagen"/>) plus its hand-authored <see cref="Application.Reporting.FieldMapping"/>;</item>
///   <item>any other form gets a generated content-controlled placeholder
///   (<see cref="PlaceholderTemplateBuilder"/>).</item>
/// </list>
/// It only ever acts on a form whose <c>WordFilePath</c> is missing or gone — once a template is
/// linked (here or via the Sprint 10 "Vorlage hochladen" wizard) it is never overwritten.
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

            try
            {
                if (AmtlicheVorlagen.Hat(formular.Anzeigename))
                {
                    await LinkAmtlicheVorlageAsync(formular, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    LinkPlaceholder(formular);
                }

                await formulare.SaveAsync(formular, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Vorlage für {Formular} konnte nicht bereitgestellt werden", formular.Anzeigename);
            }
        }
    }

    private static async Task LinkAmtlicheVorlageAsync(Formular formular, CancellationToken cancellationToken)
    {
        var dateiname = AmtlicheVorlagen.Dateiname(formular.Anzeigename);
        var ziel = Path.Combine(ZeugnisAppPaths.TemplatesDirectory, dateiname);

        await using (var quelle = AmtlicheVorlagen.Oeffnen(dateiname))
        await using (var datei = File.Create(ziel))
        {
            await quelle.CopyToAsync(datei, cancellationToken).ConfigureAwait(false);
        }

        formular.WordFilePath = ziel;
        formular.FieldMappingJson = AmtlicheVorlagen.Zuordnung(formular).ToJson();
        Log.Information("Amtliche Vorlage für {Formular} verknüpft: {Path}", formular.Anzeigename, ziel);
    }

    private static void LinkPlaceholder(Formular formular)
    {
        var path = Path.Combine(ZeugnisAppPaths.TemplatesDirectory, $"{SafeName(formular.Anzeigename)}.docx");
        var mapping = PlaceholderTemplateBuilder.Build(path, formular);
        formular.WordFilePath = path;
        formular.FieldMappingJson = mapping.ToJson();
        Log.Information("Platzhalter-Vorlage für {Formular} erzeugt: {Path}", formular.Anzeigename, path);
    }

    private static string SafeName(string value)
    {
        var cleaned = string.Concat(value.Select(c => char.IsLetterOrDigit(c) || c is ' ' or '-' or '_' ? c : '_')).Trim();
        return string.IsNullOrEmpty(cleaned) ? "Formular" : cleaned;
    }
}
