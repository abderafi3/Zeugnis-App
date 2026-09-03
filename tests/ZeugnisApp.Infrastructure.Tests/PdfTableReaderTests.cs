using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;
using ZeugnisApp.Infrastructure.Import;

namespace ZeugnisApp.Infrastructure.Tests;

public class PdfTableReaderTests
{
    // Mimics a SchILD "Schüler-Abwesenheit – Zusammenfassung": a header band, one plain row, one row
    // whose name wraps onto two lines with the figures sitting between them, and a "Summe" footer.
    private static byte[] BuildAbsencePdf()
    {
        var builder = new PdfDocumentBuilder();
        var font = builder.AddStandard14Font(Standard14Font.Helvetica);
        var page = builder.AddPage(595, 842);

        void T(string text, double x, double y) => page.AddText(text, 10, new PdfPoint(x, y), font);

        // Title line with a date (must not leak into the header row).
        T("Schueler-Abwesenheit, 01.08. - 02.09.2026", 50, 810);

        // Header band.
        T("Schueler", 50, 780);
        T("Anwesend", 300, 780);
        T("Abwesend", 372, 780);
        T("Verspaetung", 446, 780);

        // Plain row.
        T("Anna Beispiel", 50, 745);
        T("120", 305, 745);
        T("4", 380, 745);
        T("1", 452, 745);

        // Wrapped-name row: figures sit vertically between the two name lines.
        T("Bruno Carlo", 50, 712);
        T("98", 305, 705);
        T("0", 380, 705);
        T("2", 452, 705);
        T("de Vries", 50, 698);

        // Footer total.
        T("Summe", 50, 660);
        T("218", 305, 660);
        T("4", 380, 660);
        T("3", 452, 660);

        return builder.Build();
    }

    [Fact]
    public void Rebuilds_rows_columns_and_wrapped_names_from_a_tabular_pdf()
    {
        using var stream = new MemoryStream(BuildAbsencePdf());
        var grid = new TabularImport().FromPdf(stream);

        // header + 2 pupils, "Summe" dropped.
        Assert.Equal(3, grid.RowCount);

        var header = grid.Rows[0];
        Assert.Contains(header, c => c.Contains("Schueler", StringComparison.Ordinal));
        Assert.Contains(header, c => c.Contains("Abwesend", StringComparison.Ordinal));
        Assert.DoesNotContain(header, c => c.Contains("2026", StringComparison.Ordinal));

        var anna = grid.Rows[1];
        Assert.Equal("Anna Beispiel", anna[0]);

        var bruno = grid.Rows[2];
        Assert.Equal("Bruno Carlo de Vries", bruno[0]);

        // The "Abwesend" column carries 4 / 0 for the two pupils.
        var abwesendCol = Enumerable.Range(1, header.Count - 1)
            .First(c => header[c].Contains("Abwesend", StringComparison.Ordinal));
        Assert.Equal("4", anna[abwesendCol]);
        Assert.Equal("0", bruno[abwesendCol]);
    }
}
