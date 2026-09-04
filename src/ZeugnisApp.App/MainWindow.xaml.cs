using System.Windows;
using Serilog;
using Wpf.Ui;
using Wpf.Ui.Controls;
using ZeugnisApp.App.Resources;
using ZeugnisApp.App.Services;
using ZeugnisApp.App.ViewModels;
using ZeugnisApp.App.ViewModels.Klassen;
using ZeugnisApp.App.Views;

namespace ZeugnisApp.App;

public partial class MainWindow : FluentWindow
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IBusyService _busy;
    private ViewModelBase? _currentPageViewModel;
    private IDisposable? _navigationBusyScope;

    public MainWindow(
        MainWindowViewModel viewModel,
        IServiceProvider serviceProvider,
        IPageService pageService,
        INavigationService navigationService,
        IBusyService busy)
    {
        _serviceProvider = serviceProvider;
        _busy = busy;

        InitializeComponent();
        DataContext = viewModel;

        RootNavigation.SetPageService(pageService);
        navigationService.SetNavigationControl(RootNavigation);

        RootNavigation.Navigating += OnNavigating;
        RootNavigation.Navigated += OnNavigated;
        Loaded += (_, _) => RootNavigation.Navigate(typeof(DashboardPage));
    }

    /// <summary>
    /// Opens the busy scope as soon as a nav item is clicked; <see cref="OnNavigated"/> closes it
    /// once the new page is actually on screen.
    /// </summary>
    private void OnNavigating(NavigationView sender, NavigatingCancelEventArgs args)
    {
        // A navigation that never completed (cancelled, or superseded by a faster click) must not
        // leak its scope and pin the overlay open.
        _navigationBusyScope?.Dispose();
        _navigationBusyScope = _busy.Begin(Strings.Busy_Opening);
    }

    /// <summary>
    /// After a page is shown, give its view model the chance to load data
    /// (plan §3 — the view stays logic-free).
    /// </summary>
    private async void OnNavigated(NavigationView sender, NavigatedEventArgs args)
    {
        // Hand the scope opened in OnNavigating over to this handler; RunAsync nests inside it so
        // the overlay never drops in the gap between the two.
        using var scope = _navigationBusyScope ?? _busy.Begin(Strings.Busy_Opening);
        _navigationBusyScope = null;

        await _busy.RunAsync(async () =>
        {
            // Persist any buffered/debounced edits on the page we are leaving (e.g. the Fehlzeiten table).
            if (_currentPageViewModel is IAsyncFlushable flushable)
            {
                try
                {
                    await flushable.FlushAsync();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Fehler beim Speichern der vorherigen Seite");
                }
            }

            _currentPageViewModel = (args.Page as FrameworkElement)?.DataContext as ViewModelBase;

            if (_currentPageViewModel is { } vm)
            {
                try
                {
                    await vm.OnNavigatedToAsync();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Fehler beim Laden der Seite {Page}", args.Page.GetType().Name);
                }
            }
        });
    }
}
