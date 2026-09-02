using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Wpf.Ui;
using ZeugnisApp.App.Services;
using ZeugnisApp.App.ViewModels;
using ZeugnisApp.App.Views;
using ZeugnisApp.Application.Abstractions;
using ZeugnisApp.Infrastructure;
using ZeugnisApp.Infrastructure.Security;
using ZeugnisApp.Reporting;

namespace ZeugnisApp.App;

public partial class App : System.Windows.Application
{
    private IHost? _host;

    public App()
    {
        // The whole UI is German: dates as TT.MM.JJJJ, decimal comma, week starts Monday.
        var culture = new CultureInfo("de-DE");
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
        FrameworkElement.LanguageProperty.OverrideMetadata(
            typeof(FrameworkElement),
            new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(culture.IetfLanguageTag)));

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(
                Path.Combine(ZeugnisAppPaths.LogsDirectory, "zeugnisapp-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14,
                shared: true,
                formatProvider: CultureInfo.InvariantCulture)
            .WriteTo.Debug(formatProvider: CultureInfo.InvariantCulture)
            .CreateLogger();
    }

    private static IHost BuildHost(string databaseKey)
        => Host.CreateDefaultBuilder()
            .UseSerilog()
            .ConfigureServices((_, services) =>
            {
                // ---- Data + infrastructure (plan §3) ----
                services.AddZeugnisInfrastructure(ZeugnisAppPaths.BuildConnectionString(key: databaseKey));
                services.AddSingleton(new DatabaseKey(databaseKey));
                services.AddZeugnisReporting();

                // ---- Navigation (WPF-UI) ----
                services.AddSingleton<IPageService, PageService>();
                services.AddSingleton<INavigationService, NavigationService>();

                // ---- App-wide services ----
                services.AddSingleton<ISaveStatusService, SaveStatusService>();
                services.AddSingleton<IDialogService, DialogService>();
                services.AddSingleton<IFilePickerService, FilePickerService>();
                services.AddSingleton<IClipboardService, ClipboardService>();
                services.AddSingleton<IFileLauncher, FileLauncher>();
                services.AddSingleton<IFloskelPickerService, FloskelPickerService>();
                services.AddSingleton<ITemplateUploadService, TemplateUploadService>();
                services.AddSingleton<ImportServices>();
                services.AddSingleton<ReportGenerationServices>();
                services.AddSingleton<TemplateBootstrapper>();

                // ---- Shell ----
                services.AddSingleton<MainWindow>();
                services.AddSingleton<MainWindowViewModel>();

                // ---- Pages + view models (one per screen, plan §7.2) ----
                services.AddSingleton<DashboardPage>();
                services.AddSingleton<DashboardViewModel>();
                services.AddSingleton<EinstellungenPage>();
                services.AddSingleton<EinstellungenViewModel>();

                services.AddSingleton<KlassenPage>();
                services.AddSingleton<ViewModels.Klassen.KlassenWorkspaceViewModel>();
                services.AddSingleton<FloskelnPage>();
                services.AddSingleton<ViewModels.Floskeln.FloskelnViewModel>();
                services.AddTransient<ViewModels.Floskeln.FloskelPickerViewModel>();

                services.AddSingleton<FormularePage>();
                services.AddSingleton<ViewModels.Formulare.FormulareWorkspaceViewModel>();

                services.AddSingleton<FehlzeitenPage>();
                services.AddSingleton<ViewModels.Fehlzeiten.FehlzeitenViewModel>();

                services.AddSingleton<ZeugnissePage>();
                services.AddSingleton<ViewModels.Zeugnisse.ZeugnisseWorkspaceViewModel>();

                services.AddSingleton<PapierkorbPage>();
                services.AddSingleton<ViewModels.Papierkorb.PapierkorbViewModel>();
            })
            .Build();

    private async void OnStartup(object sender, StartupEventArgs e)
    {
        try
        {
            // A staged restore is applied before the database is opened (plan §Sprint 11).
            PendingRestore.ApplyIfPresent();

            var unlock = ResolveDatabaseKey();
            if (unlock is null)
            {
                Shutdown(0);
                return;
            }

            _host = BuildHost(unlock.Key);
            await _host.StartAsync();

            // Apply pending migrations before any screen opens (plan §2).
            await _host.Services.GetRequiredService<DatabaseInitializer>().InitializeAsync();

            // Link every standard Formular to the school's bundled, tagged .docx (generated
            // placeholder for any custom form) so "Zeugnis erzeugen" works from the start.
            await _host.Services.GetRequiredService<Services.TemplateBootstrapper>().EnsureTemplatesAsync();

            // Purge Papierkorb items past the retention window (plan §Sprint 11).
            var settings = await _host.Services.GetRequiredService<ISchoolSettingsService>().GetAsync();
            await _host.Services.GetRequiredService<IPapierkorbService>().PurgeExpiredAsync(settings.PapierkorbAufbewahrungTage);

            var window = _host.Services.GetRequiredService<MainWindow>();
            Services.ThemeApplier.Apply(settings.Design, window);
            window.Show();

            if (unlock.ShowRecoveryKey)
            {
                new RecoveryKeyWindow(displayKey: unlock.Key) { Owner = window }.ShowDialog();
            }
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Start der Anwendung fehlgeschlagen");
            MessageBox.Show(
                "Die Anwendung konnte nicht gestartet werden.\n\n" + ex.Message,
                "Zeugnis-App",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(-1);
        }
    }

    private async void OnExit(object sender, ExitEventArgs e)
    {
        if (_host is not null)
        {
            using (_host)
            {
                await _host.StopAsync(TimeSpan.FromSeconds(5));
            }
        }

        await Log.CloseAndFlushAsync();
    }

    private sealed record Unlock(string Key, bool ShowRecoveryKey);

    /// <summary>
    /// Unlocks the encrypted data file (plan §Sprint 11): use the DPAPI-stored key, or prompt for
    /// the recovery key when it is missing (fresh machine / after a restore). Returns null to exit.
    /// The one-time recovery-key display is deferred until after the main window is up.
    /// </summary>
    private static Unlock? ResolveDatabaseKey()
    {
        SQLitePCL.Batteries_V2.Init();
        var provider = new DpapiKeyProvider(ZeugnisAppPaths.KeyFile, ZeugnisAppPaths.DatabaseFile);

        if (provider.NeedsRecovery)
        {
            while (true)
            {
                var prompt = new RecoveryKeyWindow(displayKey: null);
                if (prompt.ShowDialog() != true)
                {
                    return null;
                }

                provider.ImportRecoveryKey(prompt.EnteredKey);
                var candidate = provider.GetOrCreateKey();
                if (CanOpen(candidate))
                {
                    return new Unlock(candidate, ShowRecoveryKey: false);
                }

                File.Delete(ZeugnisAppPaths.KeyFile);
                prompt.ShowError("Der Schlüssel ist nicht korrekt.");
            }
        }

        var key = provider.GetOrCreateKey();
        return new Unlock(key, ShowRecoveryKey: provider.WasJustCreated);
    }

    private static bool CanOpen(string key)
    {
        try
        {
            using var conn = new Microsoft.Data.Sqlite.SqliteConnection(ZeugnisAppPaths.BuildConnectionString(key: key));
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT count(*) FROM sqlite_master;";
            cmd.ExecuteScalar();
            return true;
        }
        catch (Microsoft.Data.Sqlite.SqliteException)
        {
            return false;
        }
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Log.Error(e.Exception, "Unbehandelte UI-Ausnahme");
        MessageBox.Show(
            "Es ist ein unerwarteter Fehler aufgetreten. Die Aktion wurde abgebrochen.\n\n" + e.Exception.Message,
            "Zeugnis-App",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
        e.Handled = true;
    }
}
