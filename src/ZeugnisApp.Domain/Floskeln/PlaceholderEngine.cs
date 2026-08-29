using System.Text;
using ZeugnisApp.Domain.Enums;

namespace ZeugnisApp.Domain.Floskeln;

/// <summary>
/// Resolves the placeholders in a Floskel text for one child (plan §Sprint 4):
/// <list type="bullet">
///   <item><c>[Name]</c> — the child's first name on its <em>first</em> occurrence, then the
///   nominative pronoun (er/sie) — "first sentence real name, later sentences pronouns".</item>
///   <item><c>[er/sie]</c>, <c>[ihn/sie]</c>, <c>[ihm/ihr]</c> — personal pronouns.</item>
///   <item><c>[sein/ihr]</c>, <c>[seine/ihre]</c> — possessive determiners.</item>
/// </list>
/// A placeholder written with a leading capital (<c>[Er/Sie]</c>, <c>[Sein/Ihr]</c>) forces a
/// capital; otherwise a pronoun that lands at the start of a sentence is capitalised automatically.
/// For <see cref="Geschlecht.Divers"/> (and unknown gender) every placeholder falls back to the
/// child's first name — the configurable default from plan §4.
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
            if (ch != '[')
            {
                result.Append(ch);
                i++;
                continue;
            }

            var close = template.IndexOf(']', i + 1);
            if (close < 0)
            {
                result.Append(template, i, template.Length - i);
                break;
            }

            var raw = template.Substring(i, close - i + 1);
            if (!Tokens.TryGetValue(raw, out var form))
            {
                result.Append(raw); // leave unknown placeholders untouched
                i = close + 1;
                continue;
            }

            var forceCapital = FirstLetterIsUpper(raw);
            var atSentenceStart = IsAtSentenceStart(result);
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
                if (forceCapital || atSentenceStart)
                {
                    replacement = Capitalize(replacement);
                }
            }

            result.Append(replacement);
            i = close + 1;
        }

        return result.ToString();
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
