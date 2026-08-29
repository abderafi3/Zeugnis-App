using ZeugnisApp.Domain.Enums;

namespace ZeugnisApp.Application.Import;

/// <summary>One cleaned phrase-library row, ready to become a <c>Floskel</c>.</summary>
public sealed record FloskelImportData(string Text, string Bereich, int Stufe, Klassengruppe Klassengruppe);

/// <summary>Projects validated wizard rows onto <see cref="FloskelImportData"/>.</summary>
public static class FloskelImportMapper
{
    public static FloskelImportData Map(ImportRow row)
    {
        var stufe = int.TryParse(row.Value(ImportFieldKeys.FloskelStufe).Trim(), out var n) && n is >= 1 and <= 4
            ? n
            : 1;

        var gruppe = ImportValueParser.TryParseKlassengruppe(row.Value(ImportFieldKeys.FloskelKlassengruppe), out var g)
            ? g
            : Klassengruppe.Beide;

        return new FloskelImportData(
            row.Value(ImportFieldKeys.FloskelText).Trim(),
            row.Value(ImportFieldKeys.FloskelBereich).Trim(),
            stufe,
            gruppe);
    }
}
