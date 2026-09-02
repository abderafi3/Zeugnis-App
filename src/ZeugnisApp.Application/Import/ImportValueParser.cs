using System.Globalization;
using ZeugnisApp.Domain.Enums;

namespace ZeugnisApp.Application.Import;

/// <summary>Tolerant parsing of the messy strings that arrive from real school files (plan §6).</summary>
public static class ImportValueParser
{
    private static readonly string[] DateFormats =
    [
        "dd.MM.yyyy", "d.M.yyyy", "dd.MM.yy", "d.M.yy",
        "yyyy-MM-dd", "dd/MM/yyyy", "d/M/yyyy", "dd-MM-yyyy",
    ];

    private static readonly HashSet<string> Male =
        new(StringComparer.OrdinalIgnoreCase) { "m", "männlich", "maennlich", "mann", "junge", "j", "boy", "male", "1" };

    private static readonly HashSet<string> Female =
        new(StringComparer.OrdinalIgnoreCase) { "w", "f", "weiblich", "frau", "mädchen", "maedchen", "girl", "female", "2" };

    private static readonly HashSet<string> Diverse =
        new(StringComparer.OrdinalIgnoreCase) { "d", "divers", "diverse", "x", "inter", "3" };

    public static bool LooksLikeDate(string value)
        => TryParseDate(value, out _);

    public static bool LooksLikeGeschlecht(string value)
        => TryParseGeschlecht(value, out _);

    private static readonly HashSet<string> Eingangsstufe =
        new(StringComparer.OrdinalIgnoreCase) { "e", "eingangsstufe", "sep", "1-2", "1/2", "1", "2" };

    private static readonly HashSet<string> Folgestufe =
        new(StringComparer.OrdinalIgnoreCase) { "f", "folgestufe", "3-4", "3/4", "3", "4" };

    private static readonly HashSet<string> Beide =
        new(StringComparer.OrdinalIgnoreCase) { "b", "beide", "alle", "all", "1-4", "*" };

    /// <summary>
    /// Accepts a plain year 1–4 as well as the school's labels <c>E1</c>/<c>E2</c> (Schuleingangsphase)
    /// and <c>03</c>/<c>04</c>.
    /// </summary>
    public static bool LooksLikeStufe(string value)
        => TryParseStufe(value, out _);

    /// <summary>Parses a Stufe/Jahrgang token to 1–4; returns <c>false</c> (0) for blank or unknown.</summary>
    public static bool TryParseStufe(string value, out int stufe)
    {
        stufe = 0;
        var token = value.Trim().ToUpperInvariant();
        if (token.Length == 0)
        {
            return false;
        }

        if (token is "E1" or "1" or "01") { stufe = 1; return true; }
        if (token is "E2" or "2" or "02") { stufe = 2; return true; }
        if (token is "E3" or "F1" or "3" or "03") { stufe = 3; return true; }
        if (token is "E4" or "F2" or "4" or "04") { stufe = 4; return true; }
        return false;
    }

    /// <summary>Differentiation level from the school's phrase file: 0 (neutral) to 5.</summary>
    public static bool LooksLikeNiveau(string value)
        => int.TryParse(value.Trim(), out var n) && n is >= 0 and <= 5;

    public static bool LooksLikeKlassengruppe(string value)
        => TryParseKlassengruppe(value, out _);

    public static bool TryParseKlassengruppe(string value, out Klassengruppe gruppe)
    {
        gruppe = Klassengruppe.Beide;
        var token = value.Trim();
        if (token.Length == 0 || Beide.Contains(token))
        {
            return true;
        }

        if (Eingangsstufe.Contains(token)) { gruppe = Klassengruppe.Eingangsstufe; return true; }
        if (Folgestufe.Contains(token)) { gruppe = Klassengruppe.Folgestufe; return true; }

        return false;
    }

    public static bool TryParseDate(string value, out DateOnly date)
    {
        date = default;
        value = value.Trim();
        if (value.Length == 0)
        {
            return false;
        }

        if (DateOnly.TryParseExact(value, DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
        {
            return true;
        }

        if (DateTime.TryParse(value, CultureInfo.GetCultureInfo("de-DE"), DateTimeStyles.None, out var dt))
        {
            date = DateOnly.FromDateTime(dt);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Returns true for a recognised token; <paramref name="geschlecht"/> is null only for an
    /// explicitly blank input (caller treats blank as "not provided").
    /// </summary>
    public static bool TryParseGeschlecht(string value, out Geschlecht? geschlecht)
    {
        geschlecht = null;
        var token = value.Trim();
        if (token.Length == 0)
        {
            return false;
        }

        if (Male.Contains(token)) { geschlecht = Geschlecht.Maennlich; return true; }
        if (Female.Contains(token)) { geschlecht = Geschlecht.Weiblich; return true; }
        if (Diverse.Contains(token)) { geschlecht = Geschlecht.Divers; return true; }

        return false;
    }
}
