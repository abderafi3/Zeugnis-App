using ZeugnisApp.Domain.Enums;

namespace ZeugnisApp.Application.Import;

/// <summary>One cleaned class-list row, ready to become a <c>Kind</c>.</summary>
public sealed record KindImportData(
    string Vorname,
    string Nachname,
    DateOnly? Geburtsdatum,
    Geschlecht? Geschlecht,
    int Schulbesuchsjahr = 0);

/// <summary>Outcome of a commit (plan §6, step 4 — "18 Kinder importiert, 2 übersprungen").</summary>
public sealed record ImportSummary(int Importiert, int Uebersprungen);

/// <summary>Projects validated wizard rows onto <see cref="KindImportData"/>.</summary>
public static class KinderImportMapper
{
    public static KindImportData Map(ImportRow row)
    {
        var geburtsdatum = ImportValueParser.TryParseDate(row.Value(ImportFieldKeys.Geburtsdatum), out var d)
            ? d
            : (DateOnly?)null;

        var geschlecht = ImportValueParser.TryParseGeschlecht(row.Value(ImportFieldKeys.Geschlecht), out var g)
            ? g
            : null;

        var schulbesuchsjahr = ImportValueParser.TryParseStufe(row.Value(ImportFieldKeys.Schulbesuchsjahr), out var sbj)
            ? sbj
            : 0;

        return new KindImportData(
            row.Value(ImportFieldKeys.Vorname).Trim(),
            row.Value(ImportFieldKeys.Nachname).Trim(),
            geburtsdatum,
            geschlecht,
            schulbesuchsjahr);
    }
}
