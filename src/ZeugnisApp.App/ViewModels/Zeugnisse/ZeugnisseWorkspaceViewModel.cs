using CommunityToolkit.Mvvm.ComponentModel;
using Serilog;
using ZeugnisApp.App.Resources;
using ZeugnisApp.App.Services;
using ZeugnisApp.App.ViewModels.Klassen;
using ZeugnisApp.Application.Abstractions;
using ZeugnisApp.Domain.Enums;

namespace ZeugnisApp.App.ViewModels.Zeugnisse;

public interface IZeugnisseNavigator
{
    void ShowUebersicht();
    Task EditZeugnisAsync(Guid kindId, string schuljahr, Halbjahr halbjahr);
}

/// <summary>Page-level view model for the Zeugnisse area: overview ↔ editor (plan §Sprint 8).</summary>
public sealed partial class ZeugnisseWorkspaceViewModel : ViewModelBase, IZeugnisseNavigator
{
    private readonly IZeugnisService _zeugnisse;
    private readonly IKlassenService _klassen;
    private readonly ISchoolSettingsService _settings;
    private readonly IFloskelPickerService _picker;
    private readonly ISaveStatusService _saveStatus;
    private readonly IDialogService _dialogs;
    private readonly ReportGenerationServices _reporting;

    [ObservableProperty]
    private ObservableObject? _current;

    public ZeugnisseWorkspaceViewModel(
        IZeugnisService zeugnisse, IKlassenService klassen, ISchoolSettingsService settings,
        IFloskelPickerService picker, ISaveStatusService saveStatus, IDialogService dialogs,
        ReportGenerationServices reporting)
    {
        _zeugnisse = zeugnisse;
        _klassen = klassen;
        _settings = settings;
        _picker = picker;
        _saveStatus = saveStatus;
        _dialogs = dialogs;
        _reporting = reporting;
    }

    public override Task OnNavigatedToAsync()
    {
        if (Current is null)
        {
            ShowUebersicht();
        }

        return Task.CompletedTask;
    }

    public void ShowUebersicht()
        => Swap(new ZeugnisUebersichtViewModel(_zeugnisse, _klassen, _settings, this));

    public async Task EditZeugnisAsync(Guid kindId, string schuljahr, Halbjahr halbjahr)
    {
        var ergebnis = await _zeugnisse.GetOrCreateAsync(kindId, schuljahr, halbjahr);
        if (!ergebnis.Erfolg)
        {
            await _dialogs.ConfirmAsync(Strings.Zeugnisse_Title, ergebnis.Fehler ?? "Es ist kein passendes Formular vorhanden.",
                okText: Strings.Common_Yes, cancelText: Strings.Common_No);
            return;
        }

        var full = await _zeugnisse.GetWithContextAsync(ergebnis.Zeugnis!.Id);
        if (full is null)
        {
            return;
        }

        Swap(new ZeugnisEditorViewModel(full, _zeugnisse, _picker, _saveStatus, this, _reporting));
    }

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
