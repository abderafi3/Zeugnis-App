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
