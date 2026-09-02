using System.Globalization;
using System.Reflection;
using ZeugnisApp.Domain.Entities;
using ZeugnisApp.Domain.Enums;

namespace ZeugnisApp.Infrastructure.Persistence;

/// <summary>
/// Grundschule Wehringhausen's real phrase library (~2 300 entries), bundled as an embedded
/// tab-separated resource and seeded once into an empty database (companion to
/// <see cref="FormularSeed"/>). Deleting the phrases does not re-seed them.
/// </summary>
internal static class FloskelSeed
{
    private const string ResourceName = "ZeugnisApp.Infrastructure.Persistence.SeedData.floskeln.tsv";

    public static IReadOnlyList<Floskel> Standardfloskeln()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Eingebettete Ressource '{ResourceName}' fehlt.");
        using var reader = new StreamReader(stream);

        var floskeln = new List<Floskel>();
        string? line;
        var first = true;
        while ((line = reader.ReadLine()) is not null)
        {
            if (first)
            {
                first = false; // header: Kuerzel \t Text \t Bereich \t Stufe \t Niveau \t Klassengruppe
                continue;
            }

            if (line.Length == 0)
            {
                continue;
            }

            var parts = line.Split('\t');
            if (parts.Length < 6 || parts[1].Length == 0)
            {
                continue;
            }

            floskeln.Add(new Floskel
            {
                Kuerzel = parts[0].Length == 0 ? null : parts[0],
                Text = parts[1],
                Bereich = parts[2],
                Stufe = ParseInt(parts[3], 0, 4),
                Niveau = ParseInt(parts[4], 0, 5),
                Klassengruppe = parts[5] switch
                {
                    "Eingangsstufe" => Klassengruppe.Eingangsstufe,
                    "Folgestufe" => Klassengruppe.Folgestufe,
                    _ => Klassengruppe.Beide,
                },
            });
        }

        return floskeln;
    }

    private static int ParseInt(string value, int min, int max)
        => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n)
            ? Math.Clamp(n, min, max)
            : min;
}
