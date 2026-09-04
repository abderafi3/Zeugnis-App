using System.Text;
using System.Text.RegularExpressions;

namespace ZeugnisApp.Domain.Floskeln;

/// <summary>
/// Some of the school's phrases pack a graded pick-one choice into one string with slashes —
/// <c>&#8230; erfasste Sachverhalte (oft/meist/teilweise) z&#252;gig</c> or
/// <c>stellte sich freudig/oft/teilweise neuen Lernaufgaben</c>. Each such group is really several
/// phrases. <see cref="Expand"/> turns one raw phrase into its concrete variants (the cartesian
/// product across every expandable group); a phrase with no expandable group comes back unchanged.
///
/// It is deliberately conservative — a slash is only a group separator when either
/// <list type="bullet">
///   <item>the alternatives are wrapped in parentheses: <c>&#8230; (a/b/c) &#8230;</c> — each one to
///         three words, first word lower-case; or</item>
///   <item>it is a tight run of <b>three or more single lower-case words</b> joined by slashes with
///         no spaces (an adverb scale like <c>freudig/oft/teilweise</c>).</item>
/// </list>
/// so gender pairs (<c>sie/er</c>, <c>ihre/seine</c> — resolved elsewhere by
/// <see cref="PlaceholderEngine"/>), compound terms with a capital (<c>W&#246;rterbuch/PC</c>,
/// <c>S&#228;tze/Texte</c>), two-way pairs and spaced multi-word runs
/// (<c>sicher/ stets/ mit einiger &#220;bung</c> — use parentheses for those) are left intact.
/// </summary>
public static class FloskelVariantExpander
{
    /// <summary>A phrase producing more variants than this is left unsplit (defensive).</summary>
    private const int MaxVariants = 24;

    private const int MaxAlternativesPerGroup = 4;

    /// <summary>Pronoun forms that must stay gender-driven, never a user-picked variant.</summary>
    private static readonly HashSet<string> GenderWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "er", "sie", "es", "ihn", "ihm", "ihr", "ihre", "ihrer", "ihren", "ihrem", "ihres",
        "sein", "seine", "seiner", "seinen", "seinem", "seines",
    };

    private static readonly Regex GroupPattern = new(
        // (a/b/c) — parenthesised; alternatives are anything but parens/slashes, vetted below.
        @"\((?<paren>[^()/]+(?:/[^()/]+)+)\)" +
        "|" +
        // a/b/c — a tight run of >=3 single lower-case words, not glued to a surrounding word.
        @"(?<![\p{L}\p{M}/])(?<bare>\p{Ll}[\p{L}\p{M}]*(?:/\p{Ll}[\p{L}\p{M}]*){2,})(?![\p{L}\p{M}/])",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static IReadOnlyList<string> Expand(string? phrase)
    {
        if (string.IsNullOrWhiteSpace(phrase))
        {
            return [phrase ?? string.Empty];
        }

        // Parts alternate between literal text and one pick-one choice.
        var parts = new List<string[]>();
        var literal = new StringBuilder();
        var cursor = 0;
        var variantCount = 1;

        foreach (Match m in GroupPattern.Matches(phrase))
        {
            var alternatives = ParseAlternatives(m);
            if (alternatives is null)
            {
                continue; // not a real choice — leave it in the literal run
            }

            literal.Append(phrase, cursor, m.Index - cursor);
            parts.Add([literal.ToString()]);
            literal.Clear();
            parts.Add(alternatives);
            cursor = m.Index + m.Length;

            variantCount *= alternatives.Length;
            if (variantCount > MaxVariants)
            {
                return [phrase];
            }
        }

        if (parts.Count == 0)
        {
            return [phrase];
        }

        literal.Append(phrase, cursor, phrase.Length - cursor);
        parts.Add([literal.ToString()]);

        var results = new List<string> { string.Empty };
        foreach (var choices in parts)
        {
            results = (from prefix in results
                       from choice in choices
                       select prefix + choice).ToList();
        }

        return results
            .Select(NormalizeSpaces)
            .Where(s => s.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    private static string[]? ParseAlternatives(Match m)
    {
        var raw = m.Groups["paren"].Success ? m.Groups["paren"].Value : m.Groups["bare"].Value;
        var alts = Regex.Split(raw, @"\s*/\s*").Select(a => a.Trim()).ToArray();

        if (alts.Length is < 2 or > MaxAlternativesPerGroup || !alts.All(LooksLikeAdverbAlternative))
        {
            return null;
        }

        return alts;
    }

    /// <summary>
    /// A usable alternative is one to three words whose first word is lower-case and none of which
    /// is a pronoun — so <c>freudig</c>, <c>mit einiger &#220;bung</c>, <c>&#252;ber kurze</c> pass while
    /// <c>W&#246;rterbuch/PC</c>, <c>z.B. Bild</c>, <c>sie</c> do not.
    /// </summary>
    private static bool LooksLikeAdverbAlternative(string alternative)
    {
        var words = alternative.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length is < 1 or > 3 || alternative.Length > 40)
        {
            return false;
        }

        if (!Regex.IsMatch(words[0], @"^\p{Ll}[\p{L}\p{M}-]*$"))
        {
            return false;
        }

        return words.Skip(1).All(w => Regex.IsMatch(w, @"^[\p{L}\p{M}-]+$"))
               && !words.Any(GenderWords.Contains);
    }

    private static string NormalizeSpaces(string value)
        => Regex.Replace(value, @"\s+", " ").Trim();
}
