namespace ZeugnisApp.Application.Import;

/// <summary>One cleaned absence row, ready to be matched onto a child in the class.</summary>
public sealed record FehlzeitImportData(string Name, int StundenGesamt, int? StundenUnentschuldigt);

/// <summary>Projects validated wizard rows onto <see cref="FehlzeitImportData"/>.</summary>
public static class FehlzeitenImportMapper
{
    public static FehlzeitImportData Map(ImportRow row)
    {
        var gesamt = ImportValueParser.TryParseCount(row.Value(ImportFieldKeys.FehlzeitGesamt), out var g) ? g : 0;

        int? unentschuldigt = ImportValueParser.TryParseCount(row.Value(ImportFieldKeys.FehlzeitUnentschuldigt), out var u)
            ? u
            : null;

        return new FehlzeitImportData(
            row.Value(ImportFieldKeys.FehlzeitName).Trim(),
            gesamt,
            unentschuldigt);
    }
}
