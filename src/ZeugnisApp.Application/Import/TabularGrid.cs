namespace ZeugnisApp.Application.Import;

/// <summary>
/// Format-agnostic parsed table: every cell is a trimmed, non-null string. Produced by the
/// format-specific parsers (Excel / Word table / CSV / pasted text) and consumed by the shared
/// import pipeline (plan §6).
/// </summary>
public sealed class TabularGrid(IReadOnlyList<IReadOnlyList<string>> rows)
{
    public static readonly TabularGrid Empty = new([]);

    public IReadOnlyList<IReadOnlyList<string>> Rows { get; } = rows;

    public int RowCount => Rows.Count;

    public int ColumnCount => Rows.Count == 0 ? 0 : Rows.Max(r => r.Count);

    public string Cell(int row, int column)
        => row < Rows.Count && column < Rows[row].Count ? Rows[row][column] : string.Empty;

    /// <summary>The data rows only (all rows, or all-but-the-first when <paramref name="skipHeader"/>).</summary>
    public IEnumerable<IReadOnlyList<string>> DataRows(bool skipHeader)
        => skipHeader ? Rows.Skip(1) : Rows;
}
