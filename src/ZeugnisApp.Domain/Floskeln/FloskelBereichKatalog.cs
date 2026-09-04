using ZeugnisApp.Domain.Entities;

namespace ZeugnisApp.Domain.Floskeln;

/// <summary>
/// Maps the short codes in Grundschule Wehringhausen's phrase spreadsheet (columns "Gruppe" and
/// "Fach") onto a readable <see cref="Floskel.Bereich"/>. Pure — used by the Excel import and
/// unit-tested directly.
/// </summary>
public static class FloskelBereichKatalog
{
    private static readonly Dictionary<string, string> Faecher = new(StringComparer.OrdinalIgnoreCase)
    {
        ["D"] = FloskelBereiche.Deutsch,
        ["SG"] = FloskelBereiche.Sprachgebrauch,
        ["LE"] = FloskelBereiche.Lesen,
        ["RS"] = FloskelBereiche.Rechtschreiben,
        ["M"] = FloskelBereiche.Mathematik,
        ["E"] = FloskelBereiche.Englisch,
        ["SU"] = FloskelBereiche.Sachunterricht,
        ["SP"] = FloskelBereiche.Sport,
        ["MU"] = FloskelBereiche.Musik,
        ["KU"] = FloskelBereiche.Kunst,
        ["ER"] = FloskelBereiche.EvangelischeReligionslehre,
        ["KR"] = FloskelBereiche.KatholischeReligionslehre,
    };

    private static readonly Dictionary<string, string> Gruppen = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ASV"] = FloskelBereiche.ArbeitsUndSozialverhalten,
        ["AV"] = FloskelBereiche.Arbeitsverhalten,
        ["SV"] = FloskelBereiche.Sozialverhalten,
        ["FSP"] = FloskelBereiche.Foerderschwerpunkt,
        ["ZB"] = FloskelBereiche.Bemerkungen,
    };

    /// <summary>
    /// Resolves a Bereich from the "Gruppe" and "Fach" codes. A subject group (<c>FACH</c>) defers
    /// to the <paramref name="fachCode"/>; an unknown code falls back to its own trimmed text.
    /// </summary>
    public static string Resolve(string? gruppeCode, string? fachCode)
    {
        var gruppe = (gruppeCode ?? string.Empty).Trim();
        var fach = (fachCode ?? string.Empty).Trim();

        if (gruppe.Length > 0 && !gruppe.Equals("FACH", StringComparison.OrdinalIgnoreCase)
            && Gruppen.TryGetValue(gruppe, out var byGruppe))
        {
            return byGruppe;
        }

        if (fach.Length > 0)
        {
            return Faecher.TryGetValue(fach, out var byFach) ? byFach : fach;
        }

        if (gruppe.Length > 0 && Gruppen.TryGetValue(gruppe, out var fallback))
        {
            return fallback;
        }

        return string.Empty;
    }
}
