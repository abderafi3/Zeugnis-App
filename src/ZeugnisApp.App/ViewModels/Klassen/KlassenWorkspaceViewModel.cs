using CommunityToolkit.Mvvm.ComponentModel;
using Serilog;
using ZeugnisApp.App.Services;
using ZeugnisApp.Application.Abstractions;

namespace ZeugnisApp.App.ViewModels.Klassen;

/// <summary>
/// Page-level view model for the Klassen area. Owns the currently shown sub-view model and the
/// navigation between the four steps (plan §Sprint 2). No NavigationView drill-down — a single
/// hosted <c>ContentControl</c> keeps the flow testable.
/// </summary>
public sealed partial class KlassenWorkspaceViewModel : ViewModelBase, IKlassenNavigator
{
    private readonly IKlassenService _service;
    private readonly IDialogService _dialogs;
    private readonly ISaveStatusService _saveStatus;

    [ObservableProperty]
    private ObservableObject? _current;

    public KlassenWorkspaceViewModel(IKlassenService service, IDialogService dialogs, ISaveStatusService saveStatus)
    {
        _service = service;
        _dialogs = dialogs;
        _saveStatus = saveStatus;
    }

    public override Task OnNavigatedToAsync()
    {
        if (Current is null)
        {
            ShowKlassenListe();
        }

        return Task.CompletedTask;
    }

    public void ShowKlassenListe()
        => Swap(new KlassenListViewModel(_service, _dialogs, this));

    public void EditKlasse(Guid? klasseId)
        => Swap(new KlasseEditViewModel(_service, _saveStatus, this, klasseId));

    public void ShowKinder(Guid klasseId, string klasseName)
        => Swap(new KinderListViewModel(_service, _dialogs, this, klasseId, klasseName));

    public void EditKind(Guid klasseId, string klasseName, Guid? kindId)
        => Swap(new KindEditViewModel(_service, _saveStatus, this, klasseId, klasseName, kindId));

    private async void Swap(ObservableObject next)
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
    }
}

/// <summary>Sub view models that load data when they become the current view.</summary>
public interface IAsyncInitializable
{
    Task InitializeAsync();
}
