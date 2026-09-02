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

    public void Dispose()
    {
        _dispatcher.InvokeShutdown();
        _thread.Join(TimeSpan.FromSeconds(10));
    }
}

[CollectionDefinition("WpfRender", DisableParallelization = true)]
public sealed class WpfRenderTestGroup : ICollectionFixture<WpfDispatcherFixture>;
