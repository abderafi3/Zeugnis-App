using CommunityToolkit.Mvvm.ComponentModel;
using Serilog;
using ZeugnisApp.App.Services;
using ZeugnisApp.App.ViewModels.Klassen;

namespace ZeugnisApp.App.ViewModels;

/// <summary>
/// Shared plumbing for the areas that host a master/detail flow in one <c>ContentControl</c>
/// (Klassen, Zeugnisse, Formulare — plan §Sprint 2). Owns the currently shown sub view model and
/// the swap between them: flush what is being left, show the next screen, then let it load.
/// The swap runs under the loading overlay, which stays up until the new screen is on screen.
/// </summary>
public abstract partial class WorkspaceViewModelBase(IBusyService busy) : ViewModelBase
{
    [ObservableProperty]
    private ObservableObject? _current;

    protected IBusyService Busy { get; } = busy;

    /// <summary>Replaces the shown screen. Fire-and-forget by design: called straight from commands.</summary>
    protected async void Swap(ObservableObject next) => await Busy.RunAsync(async () =>
    {
        // Persist any buffered edits from the screen we are leaving.
        if (Current is IAsyncFlushable flushable)
        {
            try
            {
                await flushable.FlushAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Autospeichern beim Verlassen der Seite fehlgeschlagen");
            }
        }

        Current = next;

        if (next is IAsyncInitializable init)
        {
            try
            {
                await init.InitializeAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Laden der Ansicht {View} fehlgeschlagen", next.GetType().Name);
            }
        }
    });
}
