using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using ZeugnisApp.App.Services;
using ZeugnisApp.App.ViewModels;
using ZeugnisApp.Application.Abstractions;

namespace ZeugnisApp.App.Tests;

/// <summary>
/// Parses the real shell XAML and forces a layout pass. A typo in MainWindow.xaml only fails at
/// runtime — and there it takes the whole app down on startup rather than one screen.
/// </summary>
[Collection("WpfRender")]
public sealed class MainWindowRenderTests(WpfDispatcherFixture wpf)
{
    private sealed class Clock : IClock
    {
        public DateTime UtcNow => new(2026, 9, 4, 12, 0, 0, DateTimeKind.Utc);
        public DateOnly Today => new(2026, 9, 4);
    }

    private static MainWindow BuildShell(IBusyService busy)
    {
        var services = new ServiceCollection().BuildServiceProvider();
        var vm = new MainWindowViewModel(new SaveStatusService(new Clock()), busy);
        var window = new MainWindow(vm, services, new PageService(services), new Wpf.Ui.NavigationService(services), busy);

        window.Measure(new Size(1200, 800));
        window.Arrange(new Rect(0, 0, 1200, 800));
        window.UpdateLayout();
        return window;
    }

    private static FrameworkElement Overlay(MainWindow window)
        => (FrameworkElement)window.FindName("BusyOverlay");

    [Fact]
    public void Shell_lays_out_with_the_busy_overlay_hidden_while_nothing_is_loading()
    {
        MainWindow? window = null;
        wpf.Invoke(() => window = BuildShell(new BusyService(TimeSpan.FromMilliseconds(30))));

        wpf.Pump(TimeSpan.FromMilliseconds(200));

        wpf.Invoke(() =>
        {
            window!.UpdateLayout();
            Assert.Equal(Visibility.Collapsed, Overlay(window).Visibility);
        });
    }

    [Fact]
    public void Busy_overlay_becomes_visible_while_a_scope_is_open()
    {
        BusyService? busy = null;
        MainWindow? window = null;

        wpf.Invoke(() =>
        {
            busy = new BusyService(TimeSpan.FromMilliseconds(30));
            window = BuildShell(busy);
            busy.Begin("Zeugnis wird geöffnet …");
        });

        wpf.PumpUntil(() => busy!.IsBusy, TimeSpan.FromSeconds(2));

        wpf.Invoke(() =>
        {
            window!.UpdateLayout();
            Assert.Equal(Visibility.Visible, Overlay(window).Visibility);
        });
    }
}
