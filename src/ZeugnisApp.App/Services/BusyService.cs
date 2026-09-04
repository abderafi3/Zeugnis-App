using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using ZeugnisApp.App.Resources;

namespace ZeugnisApp.App.Services;

/// <summary>
/// Backs the app-wide "Wird geladen …" overlay. Anything that makes the user wait for a screen
/// (navigation, a workspace swap, opening the Floskel picker) wraps its work in
/// <see cref="Begin"/>, and the shell shows a spinner until the last scope is disposed.
/// </summary>
public interface IBusyService
{
    /// <summary>True while at least one scope is open (plus a short tail, see the minimum below).</summary>
    bool IsBusy { get; }

    /// <summary>The text under the spinner.</summary>
    string Message { get; }

    /// <summary>Opens a busy scope. Dispose it — ideally with <c>using</c> — when the work is done.</summary>
    IDisposable Begin(string? message = null);

    /// <summary>
    /// Runs <paramref name="work"/> under the overlay: paints it first, then holds it until what
    /// the work produced is actually on screen. The usual shape — prefer it over a bare
    /// <see cref="Begin"/> whenever the work ends in a screen the user is waiting for.
    /// </summary>
    Task RunAsync(Func<Task> work, string? message = null);
}

/// <summary>
/// Reference-counted so overlapping scopes nest (e.g. "Zeugnis öffnen" wraps the workspace swap it
/// triggers).
/// <para>
/// The overlay goes up <em>immediately</em>, with no "only if this turns out to be slow" delay, and
/// that is deliberate. Measuring the real screens showed only 20–210 ms of the wait is awaited work;
/// the rest — around 400 ms on Fehlzeiten with 194 children, more on the 2300-row Floskel library —
/// is WPF laying the rows out on the UI thread in one uninterrupted stretch. A timer cannot tick
/// during that, so a delayed spinner would have missed exactly the wait worth covering. Showing it
/// up front and letting the caller yield (see <see cref="UiThread"/>) paints it before the thread
/// goes away.
/// </para>
/// All members are UI-thread only, which is where every caller lives.
/// </summary>
public sealed partial class BusyService : ObservableObject, IBusyService
{
    /// <summary>Once shown, stay up this long so a quick screen does not strobe the overlay.</summary>
    public static readonly TimeSpan MinimumVisible = TimeSpan.FromMilliseconds(150);

    private readonly DispatcherTimer _hideTimer;
    private readonly TimeSpan _minimumVisible;
    private DateTime _visibleSinceUtc;
    private int _depth;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _message = Strings.Busy_Loading;

    public BusyService()
        : this(MinimumVisible)
    {
    }

    /// <summary>Test seam: pass <see cref="TimeSpan.Zero"/> to drop the anti-strobe tail.</summary>
    internal BusyService(TimeSpan minimumVisible)
    {
        _minimumVisible = minimumVisible;

        var dispatcher = System.Windows.Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
        _hideTimer = new DispatcherTimer(DispatcherPriority.Normal, dispatcher) { Interval = minimumVisible };
        _hideTimer.Tick += OnHideTick;
    }

    public IDisposable Begin(string? message = null)
    {
        _depth++;

        if (!string.IsNullOrEmpty(message))
        {
            Message = message;
        }
        else if (_depth == 1)
        {
            Message = Strings.Busy_Loading;
        }

        if (_depth == 1)
        {
            // A new scope opening while the previous one is serving out its minimum keeps the
            // overlay up rather than letting it blink between the two.
            _hideTimer.Stop();
            if (!IsBusy)
            {
                _visibleSinceUtc = DateTime.UtcNow;
                IsBusy = true;
            }
        }

        return new Scope(this);
    }

    public async Task RunAsync(Func<Task> work, string? message = null)
    {
        using var scope = Begin(message);

        // Painted before the work starts, because most of what follows ties up the UI thread.
        await UiThread.PaintAsync();
        await work();

        // Whatever the work queued — building rows, laying a new view out — happens after this
        // method yields, so the overlay only comes down once the dispatcher has nothing left.
        await UiThread.SettledAsync();
    }

    private void End()
    {
        if (_depth == 0)
        {
            return;
        }

        _depth--;
        if (_depth > 0 || !IsBusy)
        {
            return;
        }

        var remaining = _minimumVisible - (DateTime.UtcNow - _visibleSinceUtc);
        if (remaining <= TimeSpan.Zero)
        {
            IsBusy = false;
            return;
        }

        _hideTimer.Stop();
        _hideTimer.Interval = remaining;
        _hideTimer.Start();
    }

    private void OnHideTick(object? sender, EventArgs e)
    {
        _hideTimer.Stop();
        _hideTimer.Interval = _minimumVisible;
        if (_depth == 0)
        {
            IsBusy = false;
        }
    }

    private sealed class Scope(BusyService owner) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            owner.End();
        }
    }
}
