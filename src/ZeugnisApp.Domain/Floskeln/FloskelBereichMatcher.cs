namespace ZeugnisApp.Domain.Floskeln;

/// <summary>
/// Decides whether a Floskel's <c>Bereich</c> belongs to a report free-text field. The two label
/// sets don't line up 1:1 — a field can read "Arbeits- und Sozialverhalten" while phrases are filed
/// under "Arbeitsverhalten" or "Sozialverhalten", and a "Deutsch" field should catch "Deutsch"
/// phrases (plan §7.2). Pure so it is unit-tested directly.
/// </summary>
public static class FloskelBereichMatcher
{
    public static bool Matches(string? feldName, string? bereich)
    {
        var feld = Normalize(feldName);
        var ber = Normalize(bereich);
        if (feld.Length == 0)
        {
            return false;
        }

        // An empty Bereich is a wildcard: the phrase applies to every field.
        if (ber.Length == 0)
        {
            return true;
        }

        if (feld == ber || feld.Contains(ber, StringComparison.Ordinal) || ber.Contains(feld, StringComparison.Ordinal))
        {
            return true;
        }

        // Token-level: any word of one is a prefix (>= 4 chars) of a word of the other.
        // "arbeits" (from the field) is a prefix of "arbeitsverhalten" (the Bereich).
        var feldTokens = Tokenize(feld);
        var berTokens = Tokenize(ber);
        foreach (var a in feldTokens)
        {
            foreach (var b in berTokens)
            {
                if (a.StartsWith(b, StringComparison.Ordinal) || b.StartsWith(a, StringComparison.Ordinal))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static string Normalize(string? value)
        => (value ?? string.Empty).Trim().ToLowerInvariant();

    private static List<string> Tokenize(string value)
    {
        var tokens = new List<string>();
        var start = -1;
        for (var i = 0; i <= value.Length; i++)
        {
            var isLetter = i < value.Length && char.IsLetter(value[i]);
            if (isLetter && start < 0)
            {
                start = i;
            }
            else if (!isLetter && start >= 0)
            {
                if (i - start >= 4)
                {
                    tokens.Add(value[start..i]);
                }

                start = -1;
            }
        }

        return tokens;
    }
}
