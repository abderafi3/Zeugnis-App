using System.Windows.Threading;

namespace ZeugnisApp.App.Services;

/// <summary>
/// Hands the UI thread back to WPF mid-method. Setting a property only marks a visual dirty —
/// nothing reaches the screen until the dispatcher gets a turn — so the loading overlay has to
/// yield before the work that blocks the thread, and again afterwards before it is taken down.
/// </summary>
public static class UiThread
{
    /// <summary>Lets everything already queued lay out and paint. No-op off the UI thread (tests).</summary>
    public static async Task PaintAsync()
    {
        if (Dispatcher.FromThread(Thread.CurrentThread) is null)
        {
            return;
        }

        await Dispatcher.Yield(DispatcherPriority.Render);
    }

    /// <summary>
    /// Waits until the dispatcher has nothing left to do — layout, render and the input queue are
    /// all drained — which is the point at which the new screen is genuinely on screen.
    /// </summary>
    public static async Task SettledAsync()
    {
        if (Dispatcher.FromThread(Thread.CurrentThread) is null)
        {
            return;
        }

        await Dispatcher.Yield(DispatcherPriority.ContextIdle);
    }
}
