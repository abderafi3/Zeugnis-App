using System.IO;
using System.Security.Cryptography;
using Serilog;
using ZeugnisApp.Application.Abstractions;
using ZeugnisApp.Application.Reporting;
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
/// A standard form that is still linked to the bundled file is re-synced whenever the app ships an
/// updated template or mapping; a form the teacher pointed at their own upload (<c>WordFilePath</c>
/// elsewhere) is never touched.
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

            try
            {
                if (AmtlicheVorlagen.Hat(formular.Anzeigename))
                {
                    await SyncAmtlicheVorlageAsync(formular, cancellationToken).ConfigureAwait(false);
                }
                else if (string.IsNullOrWhiteSpace(formular.WordFilePath) || !File.Exists(formular.WordFilePath))
                {
                    LinkPlaceholder(formular);
                    await formulare.SaveAsync(formular, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Vorlage für {Formular} konnte nicht bereitgestellt werden", formular.Anzeigename);
            }
        }
    }

    private async Task SyncAmtlicheVorlageAsync(Formular formular, CancellationToken cancellationToken)
    {
        var dateiname = AmtlicheVorlagen.Dateiname(formular.Anzeigename);
        var ziel = Path.Combine(ZeugnisAppPaths.TemplatesDirectory, dateiname);

        // The teacher uploaded their own template (it lives outside the Vorlagen directory) — leave it.
        if (!string.IsNullOrWhiteSpace(formular.WordFilePath)
            && !PathEquals(formular.WordFilePath, ziel)
            && File.Exists(formular.WordFilePath))
        {
            return;
        }

        var dateiAktuell = File.Exists(ziel) && await MatchesEmbeddedAsync(ziel, dateiname, cancellationToken).ConfigureAwait(false);
        if (!dateiAktuell)
        {
            await using var quelle = AmtlicheVorlagen.Oeffnen(dateiname);
            await using var datei = File.Create(ziel);
            await quelle.CopyToAsync(datei, cancellationToken).ConfigureAwait(false);
        }

        var mapping = AmtlicheVorlagen.Zuordnung(formular);
        if (dateiAktuell && PathEquals(formular.WordFilePath, ziel) && SameMapping(formular.FieldMappingJson, mapping))
        {
            return;
        }

        var neu = string.IsNullOrWhiteSpace(formular.WordFilePath);
        formular.WordFilePath = ziel;
        formular.FieldMappingJson = mapping.ToJson();
        await formulare.SaveAsync(formular, cancellationToken).ConfigureAwait(false);

        Log.Information(
            neu ? "Amtliche Vorlage für {Formular} verknüpft: {Path}"
                : "Amtliche Vorlage für {Formular} aktualisiert: {Path}",
            formular.Anzeigename, ziel);
    }

    private static async Task<bool> MatchesEmbeddedAsync(string path, string dateiname, CancellationToken cancellationToken)
    {
        await using var embedded = AmtlicheVorlagen.Oeffnen(dateiname);
        await using var onDisk = File.OpenRead(path);
        var a = await SHA256.HashDataAsync(embedded, cancellationToken).ConfigureAwait(false);
        var b = await SHA256.HashDataAsync(onDisk, cancellationToken).ConfigureAwait(false);
        return a.AsSpan().SequenceEqual(b);
    }

    /// <summary>Order-independent equality of the stored mapping against a freshly computed one.</summary>
    private static bool SameMapping(string? storedJson, FieldMapping fresh)
    {
        var stored = FieldMapping.FromJson(storedJson).Tags;
        return stored.Count == fresh.Tags.Count
               && fresh.Tags.All(kv => stored.TryGetValue(kv.Key, out var v) && v == kv.Value);
    }

    private static bool PathEquals(string? a, string b)
        => !string.IsNullOrWhiteSpace(a)
           && string.Equals(Path.GetFullPath(a), Path.GetFullPath(b), StringComparison.OrdinalIgnoreCase);

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
