using CommunityToolkit.Mvvm.ComponentModel;
using Serilog;
using ZeugnisApp.App.Services;
using ZeugnisApp.App.ViewModels.Klassen;
using ZeugnisApp.Application.Abstractions;

namespace ZeugnisApp.App.ViewModels.Formulare;

public interface IFormulareNavigator
{
    void ShowListe();
    void Edit(Guid? formularId);
}

/// <summary>Page-level view model for the Formulare area: list ↔ edit (plan §Sprint 5).</summary>
public sealed partial class FormulareWorkspaceViewModel(IFormulareService service, IDialogService dialogs, ISaveStatusService saveStatus)
    : ViewModelBase, IFormulareNavigator
{
    [ObservableProperty]
    private ObservableObject? _current;

    public override Task OnNavigatedToAsync()
    {
        if (Current is null)
        {
            ShowListe();
        }

        return Task.CompletedTask;
    }

    public void ShowListe() => Swap(new FormulareListViewModel(service, dialogs, this));

    public void Edit(Guid? formularId) => Swap(new FormularEditViewModel(service, saveStatus, this, formularId));

    private async void Swap(ObservableObject next)
    {
        if (Current is IAsyncFlushable flushable)
        {
            try
            {
                await flushable.FlushAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Autospeichern beim Verlassen fehlgeschlagen");
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
