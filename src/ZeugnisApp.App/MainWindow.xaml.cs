using System.Windows;
using Serilog;
using Wpf.Ui;
using Wpf.Ui.Controls;
using ZeugnisApp.App.ViewModels;
using ZeugnisApp.App.ViewModels.Klassen;
using ZeugnisApp.App.Views;

namespace ZeugnisApp.App;

public partial class MainWindow : FluentWindow
{
    private readonly IServiceProvider _serviceProvider;
    private ViewModelBase? _currentPageViewModel;

    public MainWindow(
        MainWindowViewModel viewModel,
        IServiceProvider serviceProvider,
        IPageService pageService,
        INavigationService navigationService)
    {
        _serviceProvider = serviceProvider;

        InitializeComponent();
        DataContext = viewModel;

        RootNavigation.SetPageService(pageService);
        navigationService.SetNavigationControl(RootNavigation);

        RootNavigation.Navigated += OnNavigated;
        Loaded += (_, _) => RootNavigation.Navigate(typeof(DashboardPage));
    }

    /// <summary>
    /// After a page is shown, give its view model the chance to load data
    /// (plan §3 — the view stays logic-free).
    /// </summary>
    private async void OnNavigated(NavigationView sender, NavigatedEventArgs args)
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
    }
}
