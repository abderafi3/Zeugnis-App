using ZeugnisApp.App.Services;
using ZeugnisApp.App.ViewModels.Zeugnisse;
using ZeugnisApp.Application.Abstractions;
using ZeugnisApp.Domain.Entities;
using ZeugnisApp.Domain.Enums;

namespace ZeugnisApp.App.Tests;

public class ZeugnisEditorTests
{
    private sealed class SavingZeugnisService : IZeugnisService
    {
        public Zeugnis? Saved { get; private set; }
        public Task SaveAsync(Zeugnis zeugnis, CancellationToken ct = default) { Saved = zeugnis; return Task.CompletedTask; }

        public Task<ZeugnisErgebnis> GetOrCreateAsync(Guid k, string s, Halbjahr h, Guid? f = null, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Zeugnis?> GetAsync(Guid id, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Zeugnis?> GetWithContextAsync(Guid id, CancellationToken ct = default) => throw new NotSupportedException();
        public Task SetFertiggestelltAsync(Guid id, bool v, CancellationToken ct = default) => throw new NotSupportedException();
        public Task SetManuellesFormularAsync(Guid kindId, Guid? formularId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<ZeugnisUebersichtZeile>> GetUebersichtForKlasseAsync(Guid k, string s, Halbjahr h, CancellationToken ct = default) => throw new NotSupportedException();
    }

    private sealed class StubPicker(string? result) : IFloskelPickerService
    {
        public Task<string?> PickAsync(string sampleName = "Alex", Geschlecht? sampleGeschlecht = Geschlecht.Weiblich)
            => Task.FromResult(result);
    }

    private sealed class NullNavigator : IZeugnisseNavigator
    {
        public void ShowUebersicht() { }
        public Task EditZeugnisAsync(Guid kindId, string schuljahr, Halbjahr halbjahr) => Task.CompletedTask;
        public Task ShowFoerderempfehlungenAsync(Guid kindId, string kindName, string schuljahr, Halbjahr halbjahr) => Task.CompletedTask;
    }

    private sealed class RecordingEngine : IReportGenerationEngine
    {
        public ReportRenderRequest? LastRequest { get; private set; }
        public string? LastTarget { get; private set; }

        public Task<GeneratedDocumentResult> GenerateAsync(ReportRenderRequest request, string targetPath, CancellationToken ct = default)
        {
            LastRequest = request;
            LastTarget = targetPath;
            return Task.FromResult(new GeneratedDocumentResult(true, targetPath, []));
        }

        public IReadOnlyList<OverflowWarning> CheckOverflow(ReportRenderRequest request) => [];
    }

    private sealed class SaveToPicker(string? path) : IFilePickerService
    {
        public string? PickFile(string title, string filter) => null;
        public string? PickSaveFile(string title, string filter, string defaultFileName) => path;
    }

    private sealed class RecordingLauncher : IFileLauncher
    {
        public string? Opened { get; private set; }
        public void Open(string path) => Opened = path;
    }

    private sealed class StubSettings(SchoolSettings settings) : ISchoolSettingsService
    {
        public Task<SchoolSettings> GetAsync(CancellationToken ct = default) => Task.FromResult(settings);
        public Task SaveAsync(SchoolSettings s, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class TestClock : IClock
    {
        public DateTime UtcNow => new(2026, 8, 29, 12, 0, 0, DateTimeKind.Utc);
        public DateOnly Today => new(2026, 8, 29);
    }

    private static Zeugnis BuildZeugnis(Formular formular, Kind kind) => new()
    {
        KindId = kind.Id, FormularId = formular.Id, Schuljahr = "2026/27", Halbjahr = Halbjahr.Erstes,
        Kind = kind, Formular = formular,
    };

    private static Formular NotenFormular(string? wordPath = null) => new()
    {
        Anzeigename = "Klasse 3 – 1. Halbjahr", Jahrgang = 3, HatNotenblock = true,
        WordFilePath = wordPath,
        FieldMappingJson = "{}",
        Faecher = ["Deutsch", "Mathematik"],
        Freitextfelder = [new() { Name = "Arbeits- und Sozialverhalten", Reihenfolge = 0, MaxZeichen = 20 }],
    };

    private static ReportGenerationServices Reporting(
        IReportGenerationEngine engine, IFilePickerService picker, IFileLauncher launcher)
        => new(engine, picker, launcher, new StubSettings(new SchoolSettings { Name = "GS Wehringhausen" }));

    private sealed class NoFloskeln : IFloskelnService
    {
        public Task<IReadOnlyList<Floskel>> GetAsync(FloskelFilter filter, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<Floskel>>([]);
        public Task<Floskel> SaveAsync(Floskel floskel, CancellationToken ct = default) => Task.FromResult(floskel);
        public Task DeleteAsync(Guid id, CancellationToken ct = default) => Task.CompletedTask;
        public Task<IReadOnlyList<string>> GetBereicheAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<string>>([]);
        public Task<IReadOnlyList<Floskel>> GetMeistgenutzteAsync(string feldName, int stufe, int limit, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<Floskel>>([]);
        public Task RegistriereVerwendungAsync(Guid floskelId, CancellationToken ct = default) => Task.CompletedTask;
    }

    private static ZeugnisEditorViewModel NewEditor(Zeugnis z, IZeugnisService service,
        IFloskelPickerService picker, ReportGenerationServices reporting)
        => new(z, service, new NoFloskeln(), picker, new SaveStatusService(new TestClock()), new NullNavigator(), reporting);

    [Fact]
    public async Task Editor_shows_only_the_sections_the_formular_switches_enable()
    {
        var formular = NotenFormular();
        var kind = new Kind { Vorname = "Anna", Nachname = "A", Foerderstatus = Foerderstatus.Regel };
        var vm = NewEditor(BuildZeugnis(formular, kind), new SavingZeugnisService(), new StubPicker(null),
            Reporting(new RecordingEngine(), new SaveToPicker(null), new RecordingLauncher()));

        await vm.InitializeAsync();

        Assert.True(vm.ShowNoten);
        Assert.False(vm.ShowEmpfehlung);
        Assert.False(vm.ShowVersetzung);
        Assert.Equal(2, vm.Noten.Count);
        Assert.Single(vm.Freitexte);
    }

    [Fact]
    public async Task Editor_auto_inserts_the_zieldifferent_remark_into_the_bemerkungen()
    {
        var kind = new Kind { Vorname = "Tom", Nachname = "B", Foerderstatus = Foerderstatus.ZieldifferentLernen };
        var service = new SavingZeugnisService();
        var vm = NewEditor(BuildZeugnis(NotenFormular(), kind), service, new StubPicker(null),
            Reporting(new RecordingEngine(), new SaveToPicker(null), new RecordingLauncher()));

        await vm.InitializeAsync();

        Assert.True(vm.ZieldifferentHinweisEingefuegt);
        Assert.Contains("Lernen", vm.Bemerkungen!, StringComparison.Ordinal);
        Assert.Same(vm.Bemerkungen, service.Saved!.Bemerkungen);
    }

    [Fact]
    public async Task Inserting_a_floskel_appends_the_resolved_text_to_the_field()
    {
        var vm = NewEditor(BuildZeugnis(NotenFormular(), new Kind { Vorname = "Anna", Nachname = "A" }),
            new SavingZeugnisService(), new StubPicker("Anna arbeitet konzentriert."),
            Reporting(new RecordingEngine(), new SaveToPicker(null), new RecordingLauncher()));
        await vm.InitializeAsync();

        var feld = vm.Freitexte[0];
        feld.Text = "Startsatz.";
        await feld.FloskelEinfuegenCommand.ExecuteAsync(null);

        Assert.Equal("Startsatz. Anna arbeitet konzentriert.", feld.Text);
        Assert.True(feld.IstUeberlauf);
    }

    [Fact]
    public async Task Erzeugen_is_disabled_without_a_linked_template()
    {
        var vm = NewEditor(BuildZeugnis(NotenFormular(wordPath: null), new Kind { Vorname = "Anna", Nachname = "A" }),
            new SavingZeugnisService(), new StubPicker(null),
            Reporting(new RecordingEngine(), new SaveToPicker(null), new RecordingLauncher()));
        await vm.InitializeAsync();

        Assert.False(vm.KannErzeugen);
        Assert.False(vm.ErzeugenCommand.CanExecute(null));
    }

    [Fact]
    public async Task Erzeugen_generates_to_the_picked_path_saves_the_path_and_opens_it()
    {
        var engine = new RecordingEngine();
        var launcher = new RecordingLauncher();
        var service = new SavingZeugnisService();
        var kind = new Kind { Vorname = "Erika", Nachname = "Mustermann", Klasse = new Klasse { Name = "3a", Jahrgang = 3 } };
        var zeugnis = BuildZeugnis(NotenFormular(wordPath: @"C:\Vorlagen\k3.docx"), kind);

        var vm = NewEditor(zeugnis, service, new StubPicker(null),
            Reporting(engine, new SaveToPicker(@"C:\Zeugnisse\Erika.docx"), launcher));
        await vm.InitializeAsync();

        await vm.ErzeugenCommand.ExecuteAsync(null);

        Assert.Equal(@"C:\Zeugnisse\Erika.docx", engine.LastTarget);
        Assert.Same(kind, engine.LastRequest!.Kind);
        Assert.Equal("3a", engine.LastRequest.Klasse.Name);
        Assert.Equal(@"C:\Zeugnisse\Erika.docx", service.Saved!.GeneratedFilePath);
        Assert.Equal(@"C:\Zeugnisse\Erika.docx", launcher.Opened);
        Assert.NotNull(vm.ZuletztErzeugt);
    }

    [Fact]
    public async Task Erzeugen_does_nothing_when_the_save_dialog_is_cancelled()
    {
        var engine = new RecordingEngine();
        var vm = NewEditor(BuildZeugnis(NotenFormular(wordPath: @"C:\Vorlagen\k3.docx"),
                new Kind { Vorname = "Anna", Nachname = "A" }),
            new SavingZeugnisService(), new StubPicker(null),
            Reporting(engine, new SaveToPicker(null), new RecordingLauncher()));
        await vm.InitializeAsync();

        await vm.ErzeugenCommand.ExecuteAsync(null);

        Assert.Null(engine.LastTarget);
    }
}
