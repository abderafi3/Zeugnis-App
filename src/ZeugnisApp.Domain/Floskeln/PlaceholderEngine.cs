using System.Text;
using ZeugnisApp.Domain.Enums;

namespace ZeugnisApp.Domain.Floskeln;

/// <summary>
/// Resolves the placeholders in a Floskel text for one child (plan §Sprint 4). Two syntaxes are
/// understood side by side:
/// <list type="bullet">
///   <item>
///     <b>App syntax</b> — <c>[Name]</c> (the child's first name on its <em>first</em> occurrence,
///     then the nominative pronoun), <c>[er/sie]</c>, <c>[ihn/sie]</c>, <c>[ihm/ihr]</c>,
///     <c>[sein/ihr]</c>, <c>[seine/ihre]</c>.
///   </item>
///   <item>
///     <b>School syntax</b> — as used in Grundschule Wehringhausen's phrase spreadsheet:
///     <c>$Vorname$</c> for the first name (<c>$VORNAME$</c> forces upper-case, <c>$vorname$</c>
///     lower-case), and a generic gender pair <c>&amp;maskulin%feminin&amp;</c> or
///     <c>&amp;maskulin/feminin&amp;</c> — e.g. <c>&amp;Er%Sie&amp;</c>, <c>&amp;seine/ihre&amp;</c>,
///     <c>&amp;Pate%Patin&amp;</c>. The chosen side is emitted exactly as written in the token;
///     at the start of a sentence it is capitalised automatically.
///   </item>
/// </list>
/// For <see cref="Geschlecht.Divers"/> (and unknown gender) every gender placeholder falls back to
/// the child's first name — the configurable default from plan §4.
/// </summary>
public static class PlaceholderEngine
{
    private enum Form { Nominativ, Akkusativ, Dativ, PossessivN, PossessivE, Name }

    private static readonly Dictionary<string, Form> Tokens = new(StringComparer.OrdinalIgnoreCase)
    {
        ["[name]"] = Form.Name,
        ["[er/sie]"] = Form.Nominativ,
        ["[ihn/sie]"] = Form.Akkusativ,
        ["[ihm/ihr]"] = Form.Dativ,
        ["[sein/ihr]"] = Form.PossessivN,
        ["[seine/ihre]"] = Form.PossessivE,
    };

    public static string Resolve(string template, string vorname, Geschlecht? geschlecht)
    {
        if (string.IsNullOrEmpty(template))
        {
            return string.Empty;
        }

        vorname = vorname.Trim();
        var result = new StringBuilder(template.Length + 16);
        var nameAlreadyUsed = false;
        var i = 0;

        while (i < template.Length)
        {
            var ch = template[i];

            if (ch == '[')
            {
                var close = template.IndexOf(']', i + 1);
                if (close < 0)
                {
                    result.Append(template, i, template.Length - i);
                    break;
                }

                var raw = template.Substring(i, close - i + 1);
                if (Tokens.TryGetValue(raw, out var form))
                {
                    AppendResolved(result, form, raw, FirstLetterIsUpper(raw), ref nameAlreadyUsed, vorname, geschlecht);
                }
                else
                {
                    result.Append(raw); // leave unknown bracket placeholders untouched
                }

                i = close + 1;
                continue;
            }

            if (ch == '$' && TryReadDollarName(template, i, out var dollarLen, out var casing))
            {
                var name = vorname.Length == 0
                    ? template.Substring(i, dollarLen)
                    : ApplyCasing(vorname, casing);
                result.Append(name);
                nameAlreadyUsed = true;
                i += dollarLen;
                continue;
            }

            if (ch == '&' && TryReadGenderPair(template, i, out var pairLen, out var male, out var female))
            {
                string replacement;
                if (geschlecht is Geschlecht.Maennlich)
                {
                    replacement = male;
                }
                else if (geschlecht is Geschlecht.Weiblich)
                {
                    replacement = female;
                }
                else
                {
                    // Divers / unknown: no grammatical form — fall back to the child's name (plan §4).
                    replacement = vorname.Length == 0 ? template.Substring(i, pairLen) : vorname;
                }

                if (IsAtSentenceStart(result))
                {
                    replacement = Capitalize(replacement);
                }

                result.Append(replacement);
                i += pairLen;
                continue;
            }

            result.Append(ch);
            i++;
        }

        return result.ToString();
    }

    private static void AppendResolved(
        StringBuilder result, Form form, string raw, bool forceCapital,
        ref bool nameAlreadyUsed, string vorname, Geschlecht? geschlecht)
    {
        string replacement;
        if (form == Form.Name && (!nameAlreadyUsed || geschlecht is null or Geschlecht.Divers))
        {
            replacement = vorname.Length == 0 ? raw : vorname;
            nameAlreadyUsed = true;
        }
        else
        {
            var effective = form == Form.Name ? Form.Nominativ : form;
            replacement = Render(effective, geschlecht, vorname);
            if (forceCapital || IsAtSentenceStart(result))
            {
                replacement = Capitalize(replacement);
            }
        }

        result.Append(replacement);
    }

    /// <summary>
    /// Matches <c>$Vorname$</c> (and the ALL-CAPS / all-lower casing variants). Returns the token
    /// length including both <c>$</c> and how the first name should be cased.
    /// </summary>
    private static bool TryReadDollarName(string s, int start, out int length, out Casing casing)
    {
        length = 0;
        casing = Casing.AsIs;

        var close = s.IndexOf('$', start + 1);
        if (close < 0)
        {
            return false;
        }

        var inner = s.Substring(start + 1, close - start - 1);
        if (!inner.Equals("Vorname", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        casing = inner switch
        {
            _ when inner.All(char.IsUpper) => Casing.Upper,
            _ when inner.All(char.IsLower) => Casing.Lower,
            _ => Casing.AsIs,
        };
        length = close - start + 1;
        return true;
    }

    private enum Casing { AsIs, Upper, Lower }

    private static string ApplyCasing(string value, Casing casing) => casing switch
    {
        Casing.Upper => value.ToUpperInvariant(),
        Casing.Lower => value.ToLowerInvariant(),
        _ => value,
    };

    /// <summary>
    /// Matches a gender pair <c>&amp;maskulin%feminin&amp;</c> or <c>&amp;maskulin/feminin&amp;</c>.
    /// Both sides must be short, non-empty and free of sentence punctuation so a stray literal
    /// <c>&amp;</c> in running text is not mistaken for a token.
    /// </summary>
    private static bool TryReadGenderPair(string s, int start, out int length, out string male, out string female)
    {
        length = 0;
        male = female = string.Empty;

        var close = s.IndexOf('&', start + 1);
        if (close < 0 || close - start > 64)
        {
            return false;
        }

        var inner = s.Substring(start + 1, close - start - 1);
        var sep = inner.IndexOf('%');
        if (sep < 0)
        {
            sep = inner.IndexOf('/');
        }

        if (sep <= 0 || sep >= inner.Length - 1)
        {
            return false;
        }

        var left = inner[..sep].Trim();
        var right = inner[(sep + 1)..].Trim();
        if (left.Length == 0 || right.Length == 0)
        {
            return false;
        }

        if (left.AsSpan().IndexOfAny(".!?;\n") >= 0 || right.AsSpan().IndexOfAny(".!?;\n") >= 0)
        {
            return false;
        }

        male = left;
        female = right;
        length = close - start + 1;
        return true;
    }

    private static string Render(Form form, Geschlecht? geschlecht, string vorname)
        => geschlecht switch
        {
            Geschlecht.Maennlich => form switch
            {
                Form.Nominativ => "er",
                Form.Akkusativ => "ihn",
                Form.Dativ => "ihm",
                Form.PossessivN => "sein",
                Form.PossessivE => "seine",
                _ => "er",
            },
            Geschlecht.Weiblich => form switch
            {
                Form.Nominativ => "sie",
                Form.Akkusativ => "sie",
                Form.Dativ => "ihr",
                Form.PossessivN => "ihr",
                Form.PossessivE => "ihre",
                _ => "sie",
            },
            // Divers / unknown: fall back to the child's name (plan §4).
            _ => vorname.Length == 0 ? "das Kind" : vorname,
        };

    private static bool FirstLetterIsUpper(string raw)
    {
        foreach (var c in raw)
        {
            if (char.IsLetter(c))
            {
                return char.IsUpper(c);
            }
        }

        return false;
    }

    private static bool IsAtSentenceStart(StringBuilder sb)
    {
        var k = sb.Length - 1;
        while (k >= 0 && sb[k] == ' ')
        {
            k--;
        }

        return k < 0 || sb[k] is '.' or '!' or '?' or ':' or '\n';
    }

    private static string Capitalize(string value)
        => value.Length == 0 ? value : char.ToUpperInvariant(value[0]) + value[1..];
}
