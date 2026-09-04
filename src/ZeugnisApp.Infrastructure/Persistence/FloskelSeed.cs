using System.Globalization;
using System.Reflection;
using ZeugnisApp.Domain.Entities;
using ZeugnisApp.Domain.Enums;
using ZeugnisApp.Domain.Floskeln;

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

            var kuerzel = parts[0].Length == 0 ? null : parts[0];
            var bereich = parts[2];
            var stufe = ParseInt(parts[3], 0, 4);
            var niveau = ParseInt(parts[4], 0, 5);
            var gruppe = parts[5] switch
            {
                "Eingangsstufe" => Klassengruppe.Eingangsstufe,
                "Folgestufe" => Klassengruppe.Folgestufe,
                _ => Klassengruppe.Beide,
            };

            // Phrases that pack a graded pick-one choice into slashes become one Floskel per variant.
            var varianten = FloskelVariantExpander.Expand(parts[1]);
            for (var i = 0; i < varianten.Count; i++)
            {
                floskeln.Add(new Floskel
                {
                    Kuerzel = VariantenKuerzel(kuerzel, i, varianten.Count),
                    Text = varianten[i],
                    Bereich = bereich,
                    Stufe = stufe,
                    Niveau = niveau,
                    Klassengruppe = gruppe,
                });
            }
        }

        return floskeln;
    }

    /// <summary>
    /// Keeps each variant's K&#252;rzel unique (the import de-dupes on it): a single variant keeps the
    /// original, a split appends <c>a</c>, <c>b</c>, <c>c</c> &#8230; A K&#252;rzel-less row stays K&#252;rzel-less.
    /// </summary>
    private static string? VariantenKuerzel(string? kuerzel, int index, int count)
    {
        if (kuerzel is null)
        {
            return null;
        }

        return count == 1 ? kuerzel : kuerzel + (char)('a' + index);
    }

    private static int ParseInt(string value, int min, int max)
        => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n)
            ? Math.Clamp(n, min, max)
            : min;
}
