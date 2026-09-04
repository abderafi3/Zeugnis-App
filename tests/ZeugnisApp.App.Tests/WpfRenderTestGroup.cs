using System.Windows.Threading;

namespace ZeugnisApp.App.Tests;

/// <summary>
/// Owns the single process-wide WPF <see cref="App"/> and one STA thread with a running
/// dispatcher. Only one <c>System.Windows.Application</c> may exist per process and every
/// element has thread affinity, so all render tests must share this one thread.
/// </summary>
public sealed class WpfDispatcherFixture : IDisposable
{
    private readonly Thread _thread;
    private Dispatcher _dispatcher = null!;

    public WpfDispatcherFixture()
    {
        SQLitePCL.Batteries_V2.Init();

        using var ready = new ManualResetEventSlim();
        _thread = new Thread(() =>
        {
            if (System.Windows.Application.Current is null)
            {
                var app = new App();
                app.InitializeComponent();
            }

            _dispatcher = Dispatcher.CurrentDispatcher;
            ready.Set();
            Dispatcher.Run();
        })
        {
            IsBackground = true,
            Name = "WpfRenderTests",
        };
        _thread.SetApartmentState(ApartmentState.STA);
        _thread.Start();
        ready.Wait();
    }

    /// <summary>Runs <paramref name="action"/> on the shared STA/dispatcher thread and propagates exceptions.</summary>
    public void Invoke(Action action) => _dispatcher.Invoke(action);

    /// <summary>
    /// Keeps the shared dispatcher pumping for <paramref name="duration"/>. A plain
    /// <c>Thread.Sleep</c> on the test thread leaves this one parked in <c>GetMessage</c>, so
    /// DispatcherTimers never tick; posting work to it is what lets time-based UI code run.
    /// </summary>
    public void Pump(TimeSpan duration)
    {
        var deadline = DateTime.UtcNow + duration;
        while (DateTime.UtcNow < deadline)
        {
            _dispatcher.Invoke(static () => { }, DispatcherPriority.Background);
            Thread.Sleep(10);
        }
    }

    /// <summary>Pumps until <paramref name="condition"/> (evaluated on the UI thread) holds, or the timeout passes.</summary>
    public void PumpUntil(Func<bool> condition, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            var met = false;
            _dispatcher.Invoke(() => met = condition(), DispatcherPriority.Background);
            if (met)
            {
                return;
            }

            Thread.Sleep(10);
        }
    }

    public void Dispose()
    {
        _dispatcher.InvokeShutdown();
        _thread.Join(TimeSpan.FromSeconds(10));
    }
}

[CollectionDefinition("WpfRender", DisableParallelization = true)]
public sealed class WpfRenderTestGroup : ICollectionFixture<WpfDispatcherFixture>;
