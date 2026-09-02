using System.Globalization;

namespace ZeugnisApp.Application.Import;

/// <summary>Result of auto-detecting the layout of an imported grid (plan §6, step 2).</summary>
public sealed record DetectionResult(bool HasHeaderRow, IReadOnlyDictionary<int, string> ColumnRoles);

/// <summary>
/// Guesses whether the first row is a header and which target field each column holds, using
/// header-name aliases first and content heuristics as a fallback (plan §6). The teacher can
/// override every guess on the wizard's column-check step.
/// </summary>
public static class ColumnDetector
{
    private const int SampleRows = 20;

    public static DetectionResult Detect(TabularGrid grid, ImportProfile profile)
    {
        if (grid.RowCount == 0 || grid.ColumnCount == 0)
        {
            return new DetectionResult(false, new Dictionary<int, string>());
        }

        var hasHeader = LooksLikeHeaderRow(grid, profile);
        var roles = new Dictionary<int, string>();
        var used = new HashSet<string>();

        if (hasHeader)
        {
            for (var col = 0; col < grid.ColumnCount; col++)
            {
                var field = MatchHeader(grid.Cell(0, col), profile, used);
                if (field is not null)
                {
                    roles[col] = field.Key;
                    used.Add(field.Key);
                }
            }
        }

        AssignByContent(grid, profile, hasHeader, roles, used);

        for (var col = 0; col < grid.ColumnCount; col++)
        {
            roles.TryAdd(col, ImportFieldKeys.Ignore);
        }

        return new DetectionResult(hasHeader, roles);
    }

    private static bool LooksLikeHeaderRow(TabularGrid grid, ImportProfile profile)
    {
        var aliasHits = 0;
        var row0Typed = false;
        for (var col = 0; col < grid.ColumnCount; col++)
        {
            var text = Normalize(grid.Cell(0, col));
            if (text.Length == 0)
            {
                continue;
            }

            if (profile.Fields.Any(f => f.HeaderAliases.Any(a => text == a || text.Contains(a, StringComparison.Ordinal))))
            {
                aliasHits++;
            }

            if (ImportValueParser.LooksLikeDate(text) || double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
            {
                row0Typed = true;
            }
        }

        if (aliasHits >= 1 && !row0Typed)
        {
            return true;
        }

        // No alias hit: header if row 0 is all words and a later row carries typed data.
        if (grid.RowCount > 1 && !row0Typed)
        {
            for (var col = 0; col < grid.ColumnCount; col++)
            {
                if (ImportValueParser.LooksLikeDate(grid.Cell(1, col)) || ImportValueParser.LooksLikeGeschlecht(grid.Cell(1, col)))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static ImportField? MatchHeader(string rawHeader, ImportProfile profile, HashSet<string> used)
    {
        var header = Normalize(rawHeader);
        if (header.Length == 0)
        {
            return null;
        }

        ImportField? best = null;
        var bestScore = 0;
        foreach (var field in profile.Fields.Where(f => !used.Contains(f.Key)))
        {
            foreach (var alias in field.HeaderAliases)
            {
                var score = header == alias ? 100 + alias.Length
                    : header.StartsWith(alias, StringComparison.Ordinal) ? 50 + alias.Length
                    : header.Contains(alias, StringComparison.Ordinal) ? 20 + alias.Length
                    : 0;

                if (score > bestScore)
                {
                    bestScore = score;
                    best = field;
                }
            }
        }

        return best;
    }

    private static void AssignByContent(
        TabularGrid grid, ImportProfile profile, bool hasHeader,
        Dictionary<int, string> roles, HashSet<string> used)
    {
        var dataRows = grid.DataRows(hasHeader).Take(SampleRows).ToList();
        if (dataRows.Count == 0)
        {
            return;
        }

        bool ColumnMostly(int col, Func<string, bool> predicate)
        {
            var values = dataRows.Select(r => col < r.Count ? r[col] : string.Empty).Where(v => v.Length > 0).ToList();
            return values.Count > 0 && values.Count(predicate) >= Math.Ceiling(values.Count * 0.6);
        }

        // Typed fields first.
        foreach (var field in profile.Fields.Where(f => f.Looks is not null && !used.Contains(f.Key)))
        {
            for (var col = 0; col < grid.ColumnCount; col++)
            {
                if (roles.ContainsKey(col) || !ColumnMostly(col, field.Looks!))
                {
                    continue;
                }

                roles[col] = field.Key;
                used.Add(field.Key);
                break;
            }
        }

        // Remaining *required* plain-text fields fill the leftover text columns in declaration
        // order. Optional text columns (e.g. Schulbesuchsjahr) are only ever mapped by header —
        // guessing them by position would hijack an unrelated column like a leading row number.
        var textFields = profile.Fields.Where(f => f.Looks is null && f.Required && !used.Contains(f.Key)).ToList();
        if (textFields.Count == 0)
        {
            return;
        }

        var freeTextColumns = Enumerable.Range(0, grid.ColumnCount)
            .Where(col => !roles.ContainsKey(col))
            .Where(col => !ColumnMostly(col, ImportValueParser.LooksLikeDate))
            .Where(col => dataRows.Any(r => col < r.Count && r[col].Length > 0))
            .ToList();

        foreach (var (field, col) in textFields.Zip(freeTextColumns))
        {
            roles[col] = field.Key;
            used.Add(field.Key);
        }
    }

    private static string Normalize(string value)
        => value.Trim().ToLowerInvariant().Replace("_", " ", StringComparison.Ordinal).Trim();
}
