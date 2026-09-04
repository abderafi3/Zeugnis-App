using ZeugnisApp.Domain.Enums;
using ZeugnisApp.Infrastructure.Persistence;

namespace ZeugnisApp.Infrastructure.Tests;

public class FloskelSeedTests
{
    [Fact]
    public void Embedded_phrase_library_loads_and_is_well_formed()
    {
        var floskeln = FloskelSeed.Standardfloskeln();

        // The school's file has ~2 300 phrases.
        Assert.InRange(floskeln.Count, 2000, 3000);
        Assert.All(floskeln, f =>
        {
            Assert.False(string.IsNullOrWhiteSpace(f.Text));
            Assert.False(string.IsNullOrWhiteSpace(f.Bereich));
            Assert.InRange(f.Stufe, 0, 4);
            Assert.InRange(f.Niveau, 0, 5);
        });

        // Kürzel are unique, so the first-run seed and later re-imports never duplicate.
        var mitKuerzel = floskeln.Where(f => !string.IsNullOrEmpty(f.Kuerzel)).Select(f => f.Kuerzel!).ToList();
        Assert.Equal(mitKuerzel.Count, mitKuerzel.Distinct(StringComparer.OrdinalIgnoreCase).Count());

        // Spot-check the taxonomy came through.
        Assert.Contains(floskeln, f => f.Bereich == "Arbeits- und Sozialverhalten");
        Assert.Contains(floskeln, f => f.Bereich == "Deutsch" && f.Klassengruppe == Klassengruppe.Eingangsstufe);
        Assert.Contains(floskeln, f => f.Bereich == "Mathematik" && f.Stufe == 4);
        Assert.Contains(floskeln, f => f.Text.Contains("$Vorname$", StringComparison.Ordinal));
        Assert.Contains(floskeln, f => f.Text.Contains("&Er%Sie&", StringComparison.Ordinal));
    }
}
