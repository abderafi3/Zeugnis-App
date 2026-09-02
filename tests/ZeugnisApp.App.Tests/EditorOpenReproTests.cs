using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ZeugnisApp.App.Services;
using ZeugnisApp.App.ViewModels.Zeugnisse;
using ZeugnisApp.Application.Abstractions;
using ZeugnisApp.Domain.Entities;
using ZeugnisApp.Domain.Enums;
using ZeugnisApp.Infrastructure;
using ZeugnisApp.Infrastructure.Persistence;

namespace ZeugnisApp.App.Tests;

/// <summary>
/// Reproduces "press Öffnen in the Zeugnis overview" end to end against a real SQLite file:
/// GetOrCreate -> GetWithContext -> new editor VM -> InitializeAsync.
/// </summary>
public sealed class EditorOpenReproTests : IAsyncDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"zeugnis-editrepro-{Guid.NewGuid():N}.db");
    private readonly ServiceProvider _sp;

    static EditorOpenReproTests() => SQLitePCL.Batteries_V2.Init();

    public EditorOpenReproTests()
    {
        var services = new ServiceCollection();
        services.AddZeugnisInfrastructure($"Data Source={_path}");
        _sp = services.BuildServiceProvider();
    }

    private sealed class Clock : IClock
    {
        public DateTime UtcNow => new(2026, 8, 29, 12, 0, 0, DateTimeKind.Utc);
        public DateOnly Today => new(2026, 8, 29);
    }

    private sealed class StubPicker : IFloskelPickerService
    {
        public Task<string?> PickAsync(string sampleName = "Alex", Geschlecht? sampleGeschlecht = Geschlecht.Weiblich)
            => Task.FromResult<string?>(null);
    }

    private sealed class NullNavigator : IZeugnisseNavigator
    {
        public void ShowUebersicht() { }
        public Task EditZeugnisAsync(Guid kindId, string schuljahr, Halbjahr halbjahr) => Task.CompletedTask;
        public Task ShowFoerderempfehlungenAsync(Guid kindId, string kindName, string schuljahr, Halbjahr halbjahr) => Task.CompletedTask;
    }

    private sealed class StubSettings : ISchoolSettingsService
    {
        public Task<SchoolSettings> GetAsync(CancellationToken ct = default) => Task.FromResult(new SchoolSettings { Name = "GS" });
        public Task SaveAsync(SchoolSettings s, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class StubEngine : IReportGenerationEngine
    {
        public Task<GeneratedDocumentResult> GenerateAsync(ReportRenderRequest request, string targetPath, CancellationToken ct = default)
            => Task.FromResult(new GeneratedDocumentResult(true, targetPath, []));
        public IReadOnlyList<OverflowWarning> CheckOverflow(ReportRenderRequest request) => [];
    }

    private sealed class StubFilePicker : IFilePickerService
    {
        public string? PickFile(string title, string filter) => null;
        public string? PickSaveFile(string title, string filter, string defaultFileName) => null;
    }

    private sealed class NullLauncher : IFileLauncher
    {
        public void Open(string path) { }
    }

    [Theory]
    [InlineData(3, Halbjahr.Erstes)]   // "Klasse 3 – 1. Halbjahr"
    [InlineData(1, Halbjahr.Zweites)]  // "Schuleingangsphase – 1. Schulbesuchsjahr"
    public async Task Pressing_oeffnen_opens_the_editor_without_throwing(int jahrgang, Halbjahr halbjahr)
    {
        await _sp.GetRequiredService<DatabaseInitializer>().InitializeAsync();

        var factory = _sp.GetRequiredService<IDbContextFactory<ZeugnisDbContext>>();
        Guid kindId;
        await using (var ctx = factory.CreateDbContext())
        {
            var klasse = new Klasse { Name = "Testklasse", Jahrgang = jahrgang };
            var kind = new Kind { Vorname = "Mia", Nachname = "Muster", Klasse = klasse, Foerderstatus = Foerderstatus.Regel };
            ctx.AddRange(klasse, kind);
            await ctx.SaveChangesAsync();
            kindId = kind.Id;
        }

        var service = _sp.GetRequiredService<IZeugnisService>();

        // --- exactly what ZeugnisseWorkspaceViewModel.EditZeugnisAsync does ---
        var ergebnis = await service.GetOrCreateAsync(kindId, "2026/27", halbjahr);
        Assert.True(ergebnis.Erfolg, ergebnis.Fehler);

        var full = await service.GetWithContextAsync(ergebnis.Zeugnis!.Id);
        Assert.NotNull(full);

        var reporting = new ReportGenerationServices(new StubEngine(), new StubFilePicker(), new NullLauncher(), new StubSettings());
        var vm = new ZeugnisEditorViewModel(full!, service, _sp.GetRequiredService<IFloskelnService>(), new StubPicker(),
            new SaveStatusService(new Clock()), new NullNavigator(), reporting);

        await vm.InitializeAsync();

        Assert.NotEmpty(vm.Freitexte);
    }

    [Fact]
    public async Task Editor_lists_the_most_used_floskeln_next_to_the_matching_field()
    {
        await _sp.GetRequiredService<DatabaseInitializer>().InitializeAsync();

        var factory = _sp.GetRequiredService<IDbContextFactory<ZeugnisDbContext>>();
        var floskeln = _sp.GetRequiredService<IFloskelnService>();

        var wenig = await floskeln.SaveAsync(new Floskel { Text = "[Name] arbeitet selbstständig.", Bereich = "Sozialverhalten", Stufe = 3 });
        var viel = await floskeln.SaveAsync(new Floskel { Text = "[Name] ist stets hilfsbereit.", Bereich = "Sozialverhalten", Stufe = 3 });
        await floskeln.RegistriereVerwendungAsync(viel.Id);
        await floskeln.RegistriereVerwendungAsync(viel.Id);
        await floskeln.RegistriereVerwendungAsync(wenig.Id);

        Guid kindId;
        await using (var ctx = factory.CreateDbContext())
        {
            var klasse = new Klasse { Name = "3b", Jahrgang = 3 };
            var kind = new Kind { Vorname = "Lena", Nachname = "L", Klasse = klasse, Geschlecht = Geschlecht.Weiblich, Foerderstatus = Foerderstatus.Regel };
            ctx.AddRange(klasse, kind);
            await ctx.SaveChangesAsync();
            kindId = kind.Id;
        }

        var service = _sp.GetRequiredService<IZeugnisService>();
        var full = await service.GetWithContextAsync((await service.GetOrCreateAsync(kindId, "2026/27", Halbjahr.Erstes)).Zeugnis!.Id);

        var reporting = new ReportGenerationServices(new StubEngine(), new StubFilePicker(), new NullLauncher(), new StubSettings());
        var vm = new ZeugnisEditorViewModel(full!, service, floskeln, new StubPicker(),
            new SaveStatusService(new Clock()), new NullNavigator(), reporting);
        await vm.InitializeAsync();

        var sozialfeld = vm.Freitexte.First(f => f.Label.Contains("Sozialverhalten", StringComparison.Ordinal));
        // The seeded phrase library also contributes here, so assert on order, not exact count:
        // the two phrases used in this test come first, most-used before less-used, placeholders resolved.
        var soziale = sozialfeld.Vorschlaege.Select(v => v.VollText).ToList();
        Assert.Equal("Lena ist stets hilfsbereit.", soziale[0]);
        Assert.Equal("Lena arbeitet selbstständig.", soziale[1]);

        // The Sozialverhalten phrases must not leak into the free-flowing Lernentwicklung field.
        var lernentwicklung = vm.Freitexte.First(f => f.Label.StartsWith("Lernentwicklung", StringComparison.Ordinal));
        Assert.DoesNotContain("Lena ist stets hilfsbereit.", lernentwicklung.Vorschlaege.Select(v => v.VollText));
        Assert.DoesNotContain("Lena arbeitet selbstständig.", lernentwicklung.Vorschlaege.Select(v => v.VollText));
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            var factory = _sp.GetRequiredService<IDbContextFactory<ZeugnisDbContext>>();
            await using var db = factory.CreateDbContext();
            await db.Database.EnsureDeletedAsync();
        }
        catch (Exception) { /* best effort */ }

        await _sp.DisposeAsync();

        foreach (var f in Directory.GetFiles(Path.GetTempPath(), Path.GetFileName(_path) + "*"))
        {
            try { File.Delete(f); } catch (IOException) { }
        }
    }
}
