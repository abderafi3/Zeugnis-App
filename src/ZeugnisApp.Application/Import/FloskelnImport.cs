using ZeugnisApp.Domain.Enums;
using ZeugnisApp.Domain.Floskeln;

namespace ZeugnisApp.Application.Import;

/// <summary>One cleaned phrase-library row, ready to become a <c>Floskel</c>.</summary>
public sealed record FloskelImportData(
    string Text,
    string Bereich,
    int Stufe,
    int Niveau,
    Klassengruppe Klassengruppe,
    string? Kuerzel = null);

/// <summary>Projects validated wizard rows onto <see cref="FloskelImportData"/>.</summary>
public static class FloskelImportMapper
{
    public static FloskelImportData Map(ImportRow row)
    {
        var stufe = ImportValueParser.TryParseStufe(row.Value(ImportFieldKeys.FloskelStufe), out var s) ? s : 0;

        var bereich = row.Value(ImportFieldKeys.FloskelBereich).Trim();
        if (bereich.Length == 0)
        {
            bereich = FloskelBereichKatalog.Resolve(
                row.Value(ImportFieldKeys.FloskelGruppe),
                row.Value(ImportFieldKeys.FloskelFach));
        }

        var kgRaw = row.Value(ImportFieldKeys.FloskelKlassengruppe).Trim();
        var gruppe = kgRaw.Length > 0 && ImportValueParser.TryParseKlassengruppe(kgRaw, out var g)
            ? g
            : KlassengruppeFuerStufe(stufe);

        var niveau = int.TryParse(row.Value(ImportFieldKeys.FloskelNiveau).Trim(), out var n) && n is >= 0 and <= 5
            ? n
            : 0;

        var kuerzel = row.Value(ImportFieldKeys.FloskelKuerzel).Trim();

        return new FloskelImportData(
            row.Value(ImportFieldKeys.FloskelText).Trim(),
            bereich,
            stufe,
            niveau,
            gruppe,
            kuerzel.Length == 0 ? null : kuerzel);
    }

    private static Klassengruppe KlassengruppeFuerStufe(int stufe) => stufe switch
    {
        1 or 2 => Klassengruppe.Eingangsstufe,
        3 or 4 => Klassengruppe.Folgestufe,
        _ => Klassengruppe.Beide,
    };
}
