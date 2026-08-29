using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Wpf.Ui;
using ZeugnisApp.App.Services;
using ZeugnisApp.App.ViewModels;
using ZeugnisApp.App.Views;
using ZeugnisApp.Infrastructure;
using ZeugnisApp.Reporting;

namespace ZeugnisApp.App;

public partial class App : System.Windows.Application
{
    private readonly IHost _host;

    public App()
    {
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

        _host = Host.CreateDefaultBuilder()
            .UseSerilog()
            .ConfigureServices((_, services) =>
            {
                // ---- Data + infrastructure (plan §3) ----
                services.AddZeugnisInfrastructure(ZeugnisAppPaths.BuildConnectionString());
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
            })
            .Build();
    }

    private async void OnStartup(object sender, StartupEventArgs e)
    {
        try
        {
            await _host.StartAsync();

            // Apply pending migrations before any screen opens (plan §2).
            await _host.Services.GetRequiredService<DatabaseInitializer>().InitializeAsync();

            // Give every Formular a usable Word template (placeholders until the school's files
            // are uploaded in Sprint 10) so "Zeugnis erzeugen" works from the start.
            await _host.Services.GetRequiredService<Services.TemplateBootstrapper>().EnsureTemplatesAsync();

            var window = _host.Services.GetRequiredService<MainWindow>();
            window.Show();
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
        using (_host)
        {
            await _host.StopAsync(TimeSpan.FromSeconds(5));
        }

        await Log.CloseAndFlushAsync();
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
