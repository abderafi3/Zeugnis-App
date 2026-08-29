using System.IO;
using ZeugnisApp.App.Services;
using ZeugnisApp.App.ViewModels.Import;
using ZeugnisApp.App.ViewModels.Klassen;
using ZeugnisApp.Application.Abstractions;
using ZeugnisApp.Application.Import;

namespace ZeugnisApp.App.Tests;

public class ImportWizardTests
{
    private sealed class FakeParser(TabularGrid grid) : ITabularImport
    {
        public TabularGrid FromExcel(Stream stream) => grid;
        public TabularGrid FromWordTable(Stream stream) => grid;
        public TabularGrid FromCsv(Stream stream) => grid;
        public TabularGrid FromDelimitedText(string text) => grid;
    }

    private sealed class FakeClipboard(string? text) : IClipboardService
    {
        public string? GetText() => text;
    }

    private sealed class RecordingCommitter : IKinderImportService
    {
        public IReadOnlyList<KindImportData>? Received { get; private set; }
        public int ReceivedSkipped { get; private set; }

        public Task<ImportSummary> CommitAsync(Guid klasseId, IReadOnlyList<KindImportData> rows, int uebersprungen, CancellationToken ct = default)
        {
            Received = rows;
            ReceivedSkipped = uebersprungen;
            return Task.FromResult(new ImportSummary(rows.Count, uebersprungen));
        }
    }

    private sealed class NullFilePicker : IFilePickerService
    {
        public string? PickFile(string title, string filter) => null;
    }

    private sealed class NullNavigator : IKlassenNavigator
    {
        public void ShowKlassenListe() { }
        public void EditKlasse(Guid? klasseId) { }
        public void ShowKinder(Guid klasseId, string klasseName) { }
        public void EditKind(Guid klasseId, string klasseName, Guid? kindId) { }
        public void ImportKinder(Guid klasseId, string klasseName) { }
    }

    private static TabularGrid Grid(params string[][] rows)
        => new(rows.Select(r => (IReadOnlyList<string>)r).ToList());

    private static KinderImportWizardViewModel NewWizard(TabularGrid grid, string? clipboard, RecordingCommitter committer)
        => new(new FakeParser(grid), committer, new NullFilePicker(), new FakeClipboard(clipboard),
            new NullNavigator(), Guid.NewGuid(), "E1");

    [Fact]
    public void Paste_advances_to_step_2_with_detected_mapping()
    {
        var grid = Grid(
            ["Vorname", "Nachname", "Geburtsdatum", "Geschlecht"],
            ["Max", "Mustermann", "03.05.2017", "m"]);
        var vm = NewWizard(grid, "irrelevant", new RecordingCommitter());

        vm.PasteFromClipboardCommand.Execute(null);

        Assert.Equal(2, vm.Step);
        Assert.True(vm.IsStep2);
        Assert.Equal(4, vm.ColumnMappings.Count);
        Assert.True(vm.CanContinueToPreview);
    }

    [Fact]
    public void Paste_with_empty_clipboard_shows_an_error_and_stays_on_step_1()
    {
        var vm = NewWizard(TabularGrid.Empty, clipboard: "   ", new RecordingCommitter());

        vm.PasteFromClipboardCommand.Execute(null);

        Assert.Equal(1, vm.Step);
        Assert.NotNull(vm.SourceError);
    }

    [Fact]
    public void Full_flow_commits_only_included_valid_rows()
    {
        var grid = Grid(
            ["Vorname", "Nachname", "Geburtsdatum", "Geschlecht"],
            ["Max", "Mustermann", "03.05.2017", "m"],
            ["Erika", "Musterfrau", "", "w"],
            ["Kein", "", "01.01.2018", "w"]);        // missing Nachname -> invalid
        var committer = new RecordingCommitter();
        var vm = NewWizard(grid, "x", committer);

        vm.PasteFromClipboardCommand.Execute(null);
        vm.ContinueToPreviewCommand.Execute(null);

        Assert.Equal(3, vm.Step);
        Assert.Equal(3, vm.PreviewRows.Count);
        Assert.Equal(2, vm.PreviewRows.Count(r => r.Included));

        vm.DoImportCommand.Execute(null);

        Assert.NotNull(committer.Received);
        Assert.Equal(2, committer.Received!.Count);
        Assert.Equal(1, committer.ReceivedSkipped);
        Assert.Equal("Mustermann", committer.Received[0].Nachname);
        Assert.NotNull(vm.ResultSummary);
    }

    [Fact]
    public void Fixing_a_row_in_the_preview_clears_its_error()
    {
        var row = new ImportRow(3, new Dictionary<string, string>
        {
            [ImportFieldKeys.Vorname] = "Kein",
            [ImportFieldKeys.Nachname] = "",
        });
        row.Revalidate(ImportProfiles.Kinder);
        var vm = new ImportRowViewModel(row, ImportProfiles.Kinder);

        Assert.False(vm.IsValid);

        vm.Cells.First(c => c.FieldKey == ImportFieldKeys.Nachname).Value = "Nachname";

        Assert.True(vm.IsValid);
        Assert.Equal(string.Empty, vm.ErrorText);
    }
}
