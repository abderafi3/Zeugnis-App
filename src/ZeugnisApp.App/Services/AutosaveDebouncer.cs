using System.Windows.Threading;

namespace ZeugnisApp.App.Services;

/// <summary>
/// Fires a save action a short time after the last change, so edit screens autosave
/// without a Save button (plan §7.1). Restart <see cref="Bump"/> on every edit.
/// </summary>
public sealed class AutosaveDebouncer
{
    private readonly DispatcherTimer _timer;
    private Func<Task>? _action;

    public AutosaveDebouncer(TimeSpan? delay = null)
    {
        _timer = new DispatcherTimer { Interval = delay ?? TimeSpan.FromMilliseconds(900) };
        _timer.Tick += async (_, _) => await FlushAsync();
    }

    public void Configure(Func<Task> action) => _action = action;

    public void Bump()
    {
        _timer.Stop();
        _timer.Start();
    }

    public async Task FlushAsync()
    {
        _timer.Stop();
        if (_action is { } action)
        {
            await action();
        }
    }

    public void Cancel() => _timer.Stop();
}
