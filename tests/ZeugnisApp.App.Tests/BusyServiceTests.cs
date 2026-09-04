using ZeugnisApp.App.Services;

namespace ZeugnisApp.App.Tests;

/// <summary>
/// The overlay goes up the moment work starts — most of the wait it covers is a UI-thread layout
/// pass that no timer could measure — so what these pin down is the other half: that it comes down
/// only when the last scope is gone, and never strobes on a screen that turns out to be quick.
/// Needs the shared STA thread because <see cref="BusyService"/> hides on a DispatcherTimer.
/// </summary>
[Collection("WpfRender")]
public sealed class BusyServiceTests(WpfDispatcherFixture wpf)
{
    private static readonly TimeSpan MinimumVisible = TimeSpan.FromMilliseconds(80);

    /// <summary>Comfortably past the minimum above, with the dispatcher actually running.</summary>
    private void Settle() => wpf.Pump(TimeSpan.FromMilliseconds(300));

    private BusyService Create(TimeSpan? minimumVisible = null)
    {
        BusyService? service = null;
        wpf.Invoke(() => service = new BusyService(minimumVisible ?? MinimumVisible));
        return service!;
    }

    [Fact]
    public void The_overlay_is_up_as_soon_as_a_scope_opens()
    {
        var busy = Create();

        // No dispatcher turn in between: the caller's next statement may already be blocking work.
        wpf.Invoke(() =>
        {
            busy.Begin("Wird geöffnet …");
            Assert.True(busy.IsBusy);
            Assert.Equal("Wird geöffnet …", busy.Message);
        });
    }

    [Fact]
    public void The_overlay_comes_down_once_the_scope_is_disposed()
    {
        var busy = Create();
        IDisposable? scope = null;

        wpf.Invoke(() => scope = busy.Begin());
        Settle();
        wpf.Invoke(() => Assert.True(busy.IsBusy));

        wpf.Invoke(() => scope!.Dispose());
        Settle();
        wpf.Invoke(() => Assert.False(busy.IsBusy));
    }

    [Fact]
    public void Nested_scopes_keep_the_overlay_up_until_the_last_one_is_disposed()
    {
        var busy = Create();
        IDisposable? outer = null;
        IDisposable? inner = null;

        // "Zeugnis öffnen" wraps the workspace swap it triggers — the overlay must not blink between them.
        wpf.Invoke(() => outer = busy.Begin());
        wpf.Invoke(() => inner = busy.Begin());
        Settle();
        wpf.Invoke(() => Assert.True(busy.IsBusy));

        wpf.Invoke(() => outer!.Dispose());
        Settle();
        wpf.Invoke(() => Assert.True(busy.IsBusy));

        wpf.Invoke(() => inner!.Dispose());
        Settle();
        wpf.Invoke(() => Assert.False(busy.IsBusy));
    }

    [Fact]
    public void Disposing_a_scope_twice_does_not_unbalance_the_count()
    {
        var busy = Create();
        IDisposable? outer = null;
        IDisposable? inner = null;

        wpf.Invoke(() => outer = busy.Begin());
        wpf.Invoke(() => inner = busy.Begin());
        wpf.Invoke(() =>
        {
            inner!.Dispose();
            inner.Dispose();
        });
        Settle();

        // The double dispose must not have cancelled the outer scope as well.
        wpf.Invoke(() => Assert.True(busy.IsBusy));
        wpf.Invoke(() => outer!.Dispose());
        Settle();
        wpf.Invoke(() => Assert.False(busy.IsBusy));
    }

    [Fact]
    public void Work_that_finishes_at_once_still_leaves_the_overlay_up_for_its_minimum()
    {
        // Otherwise a screen that loads in 20 ms would flash the overlay for a frame or two.
        var busy = Create(TimeSpan.FromSeconds(2));

        wpf.Invoke(() => busy.Begin().Dispose());
        wpf.Pump(TimeSpan.FromMilliseconds(300));

        wpf.Invoke(() => Assert.True(busy.IsBusy));
    }
}
