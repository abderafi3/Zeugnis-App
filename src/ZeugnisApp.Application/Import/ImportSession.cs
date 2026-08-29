namespace ZeugnisApp.Application.Import;

/// <summary>
/// Drives one run of the import wizard (plan §6): holds the parsed grid, the (editable) column
/// mapping and header flag, and rebuilds the preview rows on demand.
/// </summary>
public sealed class ImportSession
{
    public ImportSession(TabularGrid grid, ImportProfile profile)
    {
        Grid = grid;
        Profile = profile;
        var detection = ColumnDetector.Detect(grid, profile);
        HasHeaderRow = detection.HasHeaderRow;
        ColumnRoles = new Dictionary<int, string>(detection.ColumnRoles);
    }

    public TabularGrid Grid { get; }

    public ImportProfile Profile { get; }

    public bool HasHeaderRow { get; set; }

    /// <summary>column index → <see cref="ImportField.Key"/> (or <see cref="ImportFieldKeys.Ignore"/>).</summary>
    public Dictionary<int, string> ColumnRoles { get; }

    /// <summary>Header label for a column, or "Spalte N" when there is no header row.</summary>
    public string ColumnHeader(int column)
        => HasHeaderRow && Grid.Cell(0, column).Length > 0
            ? Grid.Cell(0, column)
            : $"Spalte {column + 1}";

    public IReadOnlyList<string> SampleValues(int column, int count = 3)
        => Grid.DataRows(HasHeaderRow)
            .Select(r => column < r.Count ? r[column] : string.Empty)
            .Where(v => v.Length > 0)
            .Take(count)
            .ToList();

    /// <summary>True once every required field is mapped to some column.</summary>
    public bool AllRequiredMapped
        => Profile.RequiredFields.All(f => ColumnRoles.ContainsValue(f.Key));

    public IReadOnlyList<string> UnmappedRequiredFields
        => Profile.RequiredFields.Where(f => !ColumnRoles.ContainsValue(f.Key)).Select(f => f.DisplayName).ToList();

    /// <summary>Applies the current mapping to the data rows and validates each.</summary>
    public List<ImportRow> BuildRows()
    {
        var rows = new List<ImportRow>();
        var number = HasHeaderRow ? 2 : 1;

        foreach (var raw in Grid.DataRows(HasHeaderRow))
        {
            if (raw.All(string.IsNullOrWhiteSpace))
            {
                number++;
                continue;
            }

            var values = new Dictionary<string, string>();
            foreach (var (col, key) in ColumnRoles)
            {
                if (key != ImportFieldKeys.Ignore && col < raw.Count)
                {
                    values[key] = raw[col].Trim();
                }
            }

            var row = new ImportRow(number, values);
            row.Revalidate(Profile);
            rows.Add(row);
            number++;
        }

        return rows;
    }
}
