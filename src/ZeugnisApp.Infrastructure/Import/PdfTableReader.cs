using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using ZeugnisApp.Application.Import;

namespace ZeugnisApp.Infrastructure.Import;

/// <summary>
/// Rebuilds a row/column grid from a tabular PDF (SchILD's "Schüler-Abwesenheit – Zusammenfassung"
/// and the like). PdfPig only yields positioned words, so this groups words into text lines by their
/// vertical position, derives the numeric column x-positions from the widest data line, snaps every
/// word to the nearest column (everything left of the first number is the name), and rejoins a name
/// that wrapped onto its own line. A best-effort header row is emitted first so the wizard's column
/// auto-mapping has something to match.
/// </summary>
internal static class PdfTableReader
{
    private const double LineTolerance = 4.0;   // pt: words within this vertical gap share a line
    private const double ColumnTolerance = 18.0; // pt: how near a word must sit to a column anchor

    public static TabularGrid Read(Stream stream)
    {
        // PdfPig needs a seekable stream it owns for its lifetime.
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        buffer.Position = 0;

        var lines = new List<Line>();
        using (var document = PdfDocument.Open(buffer))
        {
            foreach (var page in document.GetPages())
            {
                lines.AddRange(GroupIntoLines(page.GetWords()));
            }
        }

        if (lines.Count == 0)
        {
            return TabularGrid.Empty;
        }

        // The data line with the most stand-alone integers defines the column layout.
        var template = lines
            .Select(l => (line: l, anchors: NumericAnchors(l)))
            .Where(x => x.anchors.Count >= 2)
            .OrderByDescending(x => x.anchors.Count)
            .FirstOrDefault();

        if (template.anchors is null || template.anchors.Count == 0)
        {
            // No numeric grid found — fall back to whitespace-splitting each line.
            return new TabularGrid(lines.Select(l => (IReadOnlyList<string>)l.Words
                .Select(w => w.Text).ToList()).ToList());
        }

        var anchors = template.anchors;
        var firstNumberLeft = template.line.Words
            .Where(w => IsInteger(w.Text))
            .Select(w => w.Left)
            .DefaultIfEmpty(double.MaxValue)
            .Min();

        var firstDataTop = template.line.Top;

        var headerCells = new string?[anchors.Count + 1];
        var dataRows = new List<DataRow>();
        var orphans = new List<(double Top, string Text)>();

        foreach (var line in lines)
        {
            var name = string.Join(' ', line.Words
                    .Where(w => w.Right <= firstNumberLeft - 1)
                    .Select(w => w.Text))
                .Trim();

            var cells = new string[anchors.Count];
            var hasNumber = false;
            for (var c = 0; c < anchors.Count; c++)
            {
                var hit = line.Words
                    .Where(w => Math.Abs(WordCenter(w) - anchors[c]) <= ColumnTolerance)
                    .OrderBy(w => Math.Abs(WordCenter(w) - anchors[c]))
                    .FirstOrDefault();

                cells[c] = hit?.Text ?? string.Empty;
                if (hit is not null && IsInteger(hit.Text))
                {
                    hasNumber = true;
                }
            }

            // Above the first data row: fold every line into the header labels, dropping the report
            // title's dates and stray glyphs so the row still reads as a header.
            if (line.Top > firstDataTop + LineTolerance)
            {
                Merge(ref headerCells[0], HeaderText(name));
                for (var c = 0; c < anchors.Count; c++)
                {
                    Merge(ref headerCells[c + 1], HeaderText(cells[c]));
                }

                continue;
            }

            if (hasNumber)
            {
                dataRows.Add(new DataRow(line.Top, name, cells));
            }
            else if (name.Length > 0)
            {
                // A text-only line is half of a wrapped name; attach it to the nearest data row.
                orphans.Add((line.Top, name));
            }
        }

        // Numbers on a SchILD export sit at the vertical centre of a two-line name, so a wrapped
        // fragment may land above or below its row — snap each orphan to the closest one either way.
        foreach (var (top, text) in orphans)
        {
            var target = dataRows
                .OrderBy(r => Math.Abs(r.Top - top))
                .FirstOrDefault();

            target?.AddNamePart(prepend: top > target.Top, text);
        }

        var rows = dataRows
            .Where(r => !IsSummary(r.Name))
            .Select(r => (IReadOnlyList<string>)r.ToCells())
            .ToList();

        if (headerCells.Any(h => !string.IsNullOrWhiteSpace(h)))
        {
            if (string.IsNullOrWhiteSpace(headerCells[0]))
            {
                headerCells[0] = "Name";
            }

            rows.Insert(0, headerCells.Select(h => h ?? string.Empty).ToList());
        }

        return new TabularGrid(rows);
    }

    /// <summary>Keeps a header fragment only if it carries a word (no bare dates / numbers / glyphs).</summary>
    private static string HeaderText(string value)
    {
        var kept = string.Join(' ', value
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
            .Where(t => t.Any(char.IsLetter) && !DatePattern.IsMatch(t)));

        return kept;
    }

    private static readonly System.Text.RegularExpressions.Regex DatePattern =
        new(@"^\d{1,2}[.\-/]\d{1,2}[.\-/]\d{2,4}\.?$", System.Text.RegularExpressions.RegexOptions.Compiled);

    private static void Merge(ref string? target, string addition)
    {
        addition = addition.Trim();
        if (addition.Length == 0)
        {
            return;
        }

        target = string.IsNullOrWhiteSpace(target) ? addition : target + " " + addition;
    }

    private static List<Line> GroupIntoLines(IEnumerable<Word> words)
    {
        var lines = new List<Line>();
        foreach (var word in words.Where(w => !string.IsNullOrWhiteSpace(w.Text))
                     .OrderByDescending(w => w.BoundingBox.Top)
                     .ThenBy(w => w.BoundingBox.Left))
        {
            var center = word.BoundingBox.Bottom + (word.BoundingBox.Height / 2);
            var line = lines.FirstOrDefault(l => Math.Abs(l.Center - center) <= LineTolerance);
            if (line is null)
            {
                line = new Line(center);
                lines.Add(line);
            }

            line.Add(word);
        }

        foreach (var line in lines)
        {
            line.Sort();
        }

        return lines.OrderByDescending(l => l.Top).ToList();
    }

    private static List<double> NumericAnchors(Line line)
        => line.Words.Where(w => IsInteger(w.Text)).Select(WordCenter).OrderBy(x => x).ToList();

    private static double WordCenter(WordBox w) => (w.Left + w.Right) / 2;

    private static bool IsInteger(string text)
        => text.Length > 0 && text.All(char.IsDigit);

    private static bool IsSummary(string name)
    {
        var n = name.Trim().ToLowerInvariant();
        return n.Length == 0 || n is "summe" or "gesamt" or "summe:" or "total";
    }

    private sealed class DataRow(double top, string name, string[] cells)
    {
        public double Top { get; } = top;

        public string Name { get; private set; } = name;

        public void AddNamePart(bool prepend, string part)
        {
            part = part.Trim();
            if (part.Length == 0)
            {
                return;
            }

            Name = prepend
                ? (part + " " + Name).Trim()
                : (Name + " " + part).Trim();
        }

        public List<string> ToCells()
        {
            var row = new List<string>(cells.Length + 1) { Name };
            row.AddRange(cells);
            return row;
        }
    }

    private sealed class Line(double center)
    {
        private readonly List<WordBox> _words = [];

        public double Center { get; private set; } = center;

        public double Top { get; private set; } = double.MinValue;

        public IReadOnlyList<WordBox> Words => _words;

        public void Add(Word word)
        {
            var box = new WordBox(word.Text, word.BoundingBox.Left, word.BoundingBox.Right,
                word.BoundingBox.Bottom + (word.BoundingBox.Height / 2));
            _words.Add(box);
            Top = Math.Max(Top, box.Center);
            Center = _words.Average(w => w.Center);
        }

        public void Sort() => _words.Sort((a, b) => a.Left.CompareTo(b.Left));
    }

    private sealed record WordBox(string Text, double Left, double Right, double Center);
}
