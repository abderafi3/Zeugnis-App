using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ZeugnisApp.App.Services;
using ZeugnisApp.App.ViewModels.Zeugnisse;
using ZeugnisApp.App.Views.Zeugnisse;
using ZeugnisApp.Application.Abstractions;
using ZeugnisApp.Domain.Entities;
using ZeugnisApp.Domain.Enums;
using ZeugnisApp.Infrastructure;
using ZeugnisApp.Infrastructure.Persistence;

namespace ZeugnisApp.App.Tests;

/// <summary>
/// Loads the real <see cref="ZeugnisEditorView"/> XAML with a real, initialized view model and
/// forces a layout pass — this is the part "press Öffnen" exercises that the headless VM tests miss.
/// </summary>
public sealed class EditorViewRenderTests
{
    static EditorViewRenderTests() => SQLitePCL.Batteries_V2.Init();

    private sealed class Clock : IClock
    {
        public DateTime UtcNow => new(2026, 8, 29, 12, 0, 0, DateTimeKind.Utc);
        public DateOnly Today => new(2026, 8, 29);
    }

    private sealed class StubPicker : IFloskelPickerService
    {
        public Task<string?> PickAsync(string s = "Alex", Geschlecht? g = Geschlecht.Weiblich) => Task.FromResult<string?>(null);
    }

    private sealed class NullNavigator : IZeugnisseNavigator
    {
        public void ShowUebersicht() { }
        public Task EditZeugnisAsync(Guid k, string s, Halbjahr h) => Task.CompletedTask;
    }

    private sealed class StubSettings : ISchoolSettingsService
    {
        public Task<SchoolSettings> GetAsync(CancellationToken ct = default) => Task.FromResult(new SchoolSettings { Name = "GS" });
        public Task SaveAsync(SchoolSettings s, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class StubEngine : IReportGenerationEngine
    {
        public Task<GeneratedDocumentResult> GenerateAsync(ReportRenderRequest r, string t, CancellationToken ct = default)
            => Task.FromResult(new GeneratedDocumentResult(true, t, []));
        public IReadOnlyList<OverflowWarning> CheckOverflow(ReportRenderRequest r) => [];
    }

    private sealed class StubFilePicker : IFilePickerService
    {
        public string? PickFile(string t, string f) => null;
        public string? PickSaveFile(string t, string f, string d) => null;
    }

    private sealed class NullLauncher : IFileLauncher { public void Open(string p) { } }

    [Fact]
    public void Editor_view_renders_with_a_real_view_model()
    {
        Exception? failure = null;

        var thread = new Thread(() =>
        {
            try
            {
                // Minimal app so App.xaml resources (converters, WPF-UI dictionaries) resolve.
                if (System.Windows.Application.Current is null)
                {
                    var app = new App();
                    app.InitializeComponent();
                }

                var vm = BuildInitializedViewModelAsync().GetAwaiter().GetResult();

                var view = new ZeugnisEditorView { DataContext = vm };
                var host = new ContentControl { Content = view, Width = 1000, Height = 1400 };
                host.Measure(new Size(1000, 1400));
                host.Arrange(new Rect(0, 0, 1000, 1400));
                host.UpdateLayout();

                Dispatcher.CurrentDispatcher.InvokeShutdown();
            }
            catch (Exception ex)
            {
                failure = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join(TimeSpan.FromSeconds(60));

        Assert.True(failure is null, failure?.ToString());
    }

    private static async Task<ZeugnisEditorViewModel> BuildInitializedViewModelAsync()
    {
        var path = Path.Combine(Path.GetTempPath(), $"zeugnis-viewrepro-{Guid.NewGuid():N}.db");
        var services = new ServiceCollection();
        services.AddZeugnisInfrastructure($"Data Source={path}");
        await using var sp = services.BuildServiceProvider();

        await sp.GetRequiredService<DatabaseInitializer>().InitializeAsync();

        // A used phrase so the right-hand "häufig genutzt" sidebar renders too.
        var floskeln = sp.GetRequiredService<IFloskelnService>();
        var f = await floskeln.SaveAsync(new Floskel { Text = "[Name] arbeitet zuverlässig.", Bereich = "Sozialverhalten", Stufe = 3 });
        await floskeln.RegistriereVerwendungAsync(f.Id);

        var factory = sp.GetRequiredService<IDbContextFactory<ZeugnisDbContext>>();
        Guid kindId;
        await using (var ctx = factory.CreateDbContext())
        {
            var klasse = new Klasse { Name = "Testklasse", Jahrgang = 3 };
            var kind = new Kind { Vorname = "Mia", Nachname = "Muster", Klasse = klasse, Foerderstatus = Foerderstatus.Regel };
            ctx.AddRange(klasse, kind);
            await ctx.SaveChangesAsync();
            kindId = kind.Id;
        }

        var service = sp.GetRequiredService<IZeugnisService>();
        var ergebnis = await service.GetOrCreateAsync(kindId, "2026/27", Halbjahr.Erstes);
        var full = await service.GetWithContextAsync(ergebnis.Zeugnis!.Id);

        var reporting = new ReportGenerationServices(new StubEngine(), new StubFilePicker(), new NullLauncher(), new StubSettings());
        var vm = new ZeugnisEditorViewModel(full!, service, sp.GetRequiredService<IFloskelnService>(), new StubPicker(),
            new SaveStatusService(new Clock()), new NullNavigator(), reporting);
        await vm.InitializeAsync();

        try
        {
            await using var db = factory.CreateDbContext();
            await db.Database.EnsureDeletedAsync();
        }
        catch { /* best effort */ }

        return vm;
    }
}
