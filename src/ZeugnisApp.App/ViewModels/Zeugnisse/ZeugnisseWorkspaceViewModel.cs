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

    /// <summary>Opens the Lern- und Förderempfehlungen for a child in the given term.</summary>
    Task ShowFoerderempfehlungenAsync(Guid kindId, string kindName, string schuljahr, Halbjahr halbjahr);
}

/// <summary>Page-level view model for the Zeugnisse area: overview ↔ editor (plan §Sprint 8).</summary>
public sealed partial class ZeugnisseWorkspaceViewModel : ViewModelBase, IZeugnisseNavigator
{
    private readonly IZeugnisService _zeugnisse;
    private readonly IKlassenService _klassen;
    private readonly ISchoolSettingsService _settings;
    private readonly IFloskelnService _floskeln;
    private readonly IFloskelPickerService _picker;
    private readonly ISaveStatusService _saveStatus;
    private readonly IDialogService _dialogs;
    private readonly ReportGenerationServices _reporting;
    private readonly IFoerderempfehlungService _foerderempfehlungen;

    [ObservableProperty]
    private ObservableObject? _current;

    public ZeugnisseWorkspaceViewModel(
        IZeugnisService zeugnisse, IKlassenService klassen, ISchoolSettingsService settings,
        IFloskelnService floskeln, IFloskelPickerService picker, ISaveStatusService saveStatus,
        IDialogService dialogs, ReportGenerationServices reporting,
        IFoerderempfehlungService foerderempfehlungen)
    {
        _zeugnisse = zeugnisse;
        _klassen = klassen;
        _settings = settings;
        _floskeln = floskeln;
        _picker = picker;
        _saveStatus = saveStatus;
        _dialogs = dialogs;
        _reporting = reporting;
        _foerderempfehlungen = foerderempfehlungen;
    }

    public override async Task OnNavigatedToAsync()
    {
        // Re-entering the area refreshes the overview (new classes, changed templates); the
        // editor keeps its state so a half-finished report is never dropped on navigation.
        if (Current is null)
        {
            ShowUebersicht();
        }
        else if (Current is ZeugnisUebersichtViewModel uebersicht)
        {
            try
            {
                await uebersicht.InitializeAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Zeugnisübersicht konnte nicht aktualisiert werden");
            }
        }
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

        Swap(new ZeugnisEditorViewModel(full, _zeugnisse, _floskeln, _picker, _saveStatus, this, _reporting));
    }

    public Task ShowFoerderempfehlungenAsync(Guid kindId, string kindName, string schuljahr, Halbjahr halbjahr)
    {
        Swap(new FoerderempfehlungenViewModel(
            _foerderempfehlungen, _saveStatus, _dialogs, this, kindId, kindName, schuljahr, halbjahr));
        return Task.CompletedTask;
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
