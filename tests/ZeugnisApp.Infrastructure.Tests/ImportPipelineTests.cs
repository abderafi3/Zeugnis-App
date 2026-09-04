using System.Text;
using ClosedXML.Excel;
using ZeugnisApp.Application.Import;
using ZeugnisApp.Domain.Enums;
using ZeugnisApp.Infrastructure.Import;

namespace ZeugnisApp.Infrastructure.Tests;

public class ImportPipelineTests
{
    private static readonly TabularImport Parser = new();

    [Fact]
    public void Excel_with_german_headers_maps_all_four_roles()
    {
        using var ms = new MemoryStream();
        using (var wb = new XLWorkbook())
        {
            var ws = wb.AddWorksheet("Klasse");
            ws.Cell(1, 1).Value = "Vorname";
            ws.Cell(1, 2).Value = "Nachname";
            ws.Cell(1, 3).Value = "Geburtsdatum";
            ws.Cell(1, 4).Value = "Geschlecht";
            ws.Cell(2, 1).Value = "Max";
            ws.Cell(2, 2).Value = "Mustermann";
            ws.Cell(2, 3).Value = new DateTime(2017, 5, 3);
            ws.Cell(2, 4).Value = "m";
            ws.Cell(3, 1).Value = "Erika";
            ws.Cell(3, 2).Value = "Musterfrau";
            ws.Cell(3, 3).Value = "14.09.2016";
            ws.Cell(3, 4).Value = "w";
            wb.SaveAs(ms);
        }

        ms.Position = 0;
        var grid = Parser.FromExcel(ms);
        var session = new ImportSession(grid, ImportProfiles.Kinder);

        Assert.True(session.HasHeaderRow);
        Assert.True(session.AllRequiredMapped);
        Assert.Equal(ImportFieldKeys.Vorname, session.ColumnRoles[0]);
        Assert.Equal(ImportFieldKeys.Nachname, session.ColumnRoles[1]);
        Assert.Equal(ImportFieldKeys.Geburtsdatum, session.ColumnRoles[2]);
        Assert.Equal(ImportFieldKeys.Geschlecht, session.ColumnRoles[3]);

        var rows = session.BuildRows();
        Assert.Equal(2, rows.Count);
        Assert.All(rows, r => Assert.True(r.IsValid));

        var data = rows.Select(KinderImportMapper.Map).ToList();
        Assert.Equal(new DateOnly(2017, 5, 3), data[0].Geburtsdatum);
        Assert.Equal(Geschlecht.Maennlich, data[0].Geschlecht);
        Assert.Equal(new DateOnly(2016, 9, 14), data[1].Geburtsdatum);
    }

    [Fact]
    public void Csv_semicolon_without_header_detects_by_content_and_position()
    {
        const string csv = "Mustermann;Max;03.05.2017;m\nMusterfrau;Erika;14.09.2016;w\n";
        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var grid = Parser.FromCsv(ms);
        var session = new ImportSession(grid, ImportProfiles.Kinder);

        Assert.False(session.HasHeaderRow);
        Assert.Equal(ImportFieldKeys.Geburtsdatum, session.ColumnRoles[2]);
        Assert.Equal(ImportFieldKeys.Geschlecht, session.ColumnRoles[3]);
        // Columns 0 and 1 are the two free text columns -> vorname then nachname by declaration order.
        Assert.Equal(ImportFieldKeys.Vorname, session.ColumnRoles[0]);
        Assert.Equal(ImportFieldKeys.Nachname, session.ColumnRoles[1]);
        Assert.True(session.AllRequiredMapped);
    }

    [Fact]
    public void Class_list_with_a_Schulbesuchsjahr_column_maps_it()
    {
        const string csv =
            "Vorname;Nachname;Geschlecht;SBJ\n" +
            "Jad;Barhoum;m;2\n" +
            "Maria;Taneva;w;E1\n" +
            "Roman;Buhira;m;\n";
        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var session = new ImportSession(Parser.FromCsv(ms), ImportProfiles.Kinder);
        Assert.True(session.HasHeaderRow);
        Assert.Equal(ImportFieldKeys.Schulbesuchsjahr, session.ColumnRoles[3]);

        var data = session.BuildRows().Select(KinderImportMapper.Map).ToList();
        Assert.Equal(2, data[0].Schulbesuchsjahr);
        Assert.Equal(1, data[1].Schulbesuchsjahr);   // "E1" → 1
        Assert.Equal(0, data[2].Schulbesuchsjahr);   // blank → not set
    }

    [Fact]
    public void Clipboard_tab_separated_text_parses_into_a_grid()
    {
        const string pasted = "Vorname\tNachname\tGeb\nMax\tMustermann\t03.05.2017\n";
        var grid = Parser.FromDelimitedText(pasted);
        var session = new ImportSession(grid, ImportProfiles.Kinder);

        Assert.True(session.HasHeaderRow);
        Assert.Equal(ImportFieldKeys.Geburtsdatum, session.ColumnRoles[2]);
        Assert.Single(session.BuildRows());
    }

    [Fact]
    public void Messy_file_flags_only_the_broken_rows_and_keeps_the_rest()
    {
        // Extra leading column, mixed date formats, one row with no surname, one bad date.
        const string csv =
            "Nr;Vorname;Nachname;Geburtsdatum;Geschlecht\n" +
            "1;Max;Mustermann;03.05.2017;m\n" +
            "2;Erika;Musterfrau;2016-09-14;w\n" +
            "3;Nur Vorname;;01.01.2018;w\n" +
            "4;Tom;Bad;kein datum;m\n";
        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var session = new ImportSession(Parser.FromCsv(ms), ImportProfiles.Kinder);
        Assert.True(session.HasHeaderRow);
        Assert.Equal(ImportFieldKeys.Ignore, session.ColumnRoles[0]); // "Nr" ignored

        var rows = session.BuildRows();
        Assert.Equal(4, rows.Count);
        Assert.True(rows[0].IsValid);
        Assert.True(rows[1].IsValid);
        Assert.False(rows[2].IsValid);            // missing Nachname
        Assert.Contains(rows[2].Errors, e => e.Contains("Nachname", StringComparison.Ordinal));
        Assert.False(rows[3].IsValid);            // unparseable date
        Assert.Contains(rows[3].Errors, e => e.Contains("Geburtsdatum", StringComparison.Ordinal));

        Assert.Equal(2, rows.Count(r => r.Included));
    }

    [Fact]
    public async Task Commit_writes_only_the_valid_rows_in_one_transaction()
    {
        await using var db = new SqliteTestDatabase();
        await db.MigrateAsync();

        Guid klasseId;
        await using (var ctx = db.NewContext())
        {
            var klasse = new Domain.Entities.Klasse { Name = "E1", Jahrgang = 1 };
            ctx.Klassen.Add(klasse);
            await ctx.SaveChangesAsync();
            klasseId = klasse.Id;
        }

        var service = new KinderImportService(db.ContextFactory);
        var data = new List<KindImportData>
        {
            new("Max", "Mustermann", new DateOnly(2017, 5, 3), Geschlecht.Maennlich),
            new("Erika", "Musterfrau", null, Geschlecht.Weiblich),
        };

        var summary = await service.CommitAsync(klasseId, data, uebersprungen: 2);

        Assert.Equal(2, summary.Importiert);
        Assert.Equal(2, summary.Uebersprungen);

        await using (var ctx = db.NewContext())
        {
            Assert.Equal(2, ctx.Kinder.Count());
        }
    }
}
