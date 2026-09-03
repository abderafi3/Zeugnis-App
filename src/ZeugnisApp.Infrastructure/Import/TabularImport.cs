using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ZeugnisApp.Application.Abstractions;
using ZeugnisApp.Application.Import;

namespace ZeugnisApp.Infrastructure.Import;

/// <summary>Parses the four import sources into a <see cref="TabularGrid"/> (plan §6, step 1).</summary>
internal sealed class TabularImport : ITabularImport
{
    public TabularGrid FromExcel(Stream stream)
    {
        using var workbook = new XLWorkbook(stream);
        var sheet = workbook.Worksheets.FirstOrDefault();
        var range = sheet?.RangeUsed();
        if (range is null)
        {
            return TabularGrid.Empty;
        }

        var columnCount = range.ColumnCount();
        var rows = new List<IReadOnlyList<string>>();
        foreach (var row in range.Rows())
        {
            var cells = new List<string>(columnCount);
            for (var c = 1; c <= columnCount; c++)
            {
                cells.Add(CellText(row.Cell(c)));
            }

            rows.Add(cells);
        }

        return new TabularGrid(rows);
    }

    public TabularGrid FromWordTable(Stream stream)
    {
        using var doc = WordprocessingDocument.Open(stream, isEditable: false);
        var table = doc.MainDocumentPart?.Document.Body?.Descendants<Table>().FirstOrDefault();
        if (table is null)
        {
            return TabularGrid.Empty;
        }

        var rows = table.Elements<TableRow>()
            .Select(tr => (IReadOnlyList<string>)tr.Elements<TableCell>()
                .Select(tc => NormalizeWhitespace(tc.InnerText))
                .ToList())
            .ToList();

        return new TabularGrid(rows);
    }

    public TabularGrid FromPdf(Stream stream) => PdfTableReader.Read(stream);

    public TabularGrid FromCsv(Stream stream)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var text = reader.ReadToEnd();
        var delimiter = SniffDelimiter(text);

        var config = new CsvConfiguration(CultureInfo.GetCultureInfo("de-DE"))
        {
            Delimiter = delimiter,
            HasHeaderRecord = false,
            DetectDelimiter = false,
            BadDataFound = null,
            TrimOptions = TrimOptions.Trim,
        };

        using var csv = new CsvReader(new StringReader(text), config);
        var rows = new List<IReadOnlyList<string>>();
        while (csv.Read())
        {
            var record = csv.Parser.Record;
            if (record is null)
            {
                continue;
            }

            rows.Add(record.Select(c => (c ?? string.Empty).Trim()).ToList());
        }

        return new TabularGrid(rows);
    }

    public TabularGrid FromDelimitedText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return TabularGrid.Empty;
        }

        var delimiter = SniffDelimiter(text)[0];
        var rows = text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n')
            .Where(line => line.Length > 0)
            .Select(line => (IReadOnlyList<string>)line.Split(delimiter).Select(c => c.Trim()).ToList())
            .ToList();

        return new TabularGrid(rows);
    }

    private static string CellText(IXLCell cell)
    {
        if (cell.IsEmpty())
        {
            return string.Empty;
        }

        if (cell.DataType == XLDataType.DateTime && cell.TryGetValue<DateTime>(out var dt))
        {
            return dt.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
        }

        return cell.GetFormattedString().Trim();
    }

    private static string SniffDelimiter(string text)
    {
        var firstLine = text.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Split('\n')
            .FirstOrDefault(l => l.Trim().Length > 0) ?? string.Empty;

        var candidates = new[] { "\t", ";", ",", "|" };
        return candidates
            .OrderByDescending(d => firstLine.Count(ch => ch == d[0]))
            .First();
    }

    private static string NormalizeWhitespace(string value)
        => string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)).Trim();
}
