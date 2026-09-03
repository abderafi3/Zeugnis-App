using System.Collections.ObjectModel;
using System.Globalization;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using ZeugnisApp.App.Resources;
using ZeugnisApp.App.Services;
using ZeugnisApp.App.ViewModels.Import;
using ZeugnisApp.App.Views.Import;
using ZeugnisApp.Application;
using ZeugnisApp.Application.Abstractions;
using ZeugnisApp.Domain.Enums;

namespace ZeugnisApp.App.ViewModels.Fehlzeiten;

/// <summary>
/// The per-class Fehlzeiten-Tabelle (plan §7.2): pick a class, enter total / unexcused hours per
/// child for the current term, inline validation, autosave.
/// </summary>
public sealed partial class FehlzeitenViewModel(
    IKlassenService klassenService,
    IFehlzeitenService fehlzeitenService,
    ISchoolSettingsService settingsService,
    ISaveStatusService saveStatus,
    ImportServices import)
    : ViewModelBase
{
    private string _schuljahr = string.Empty;
    private Halbjahr _halbjahr = Halbjahr.Erstes;
    private int _reloadGeneration;
    private bool _suppressReload;

    public ObservableCollection<KlasseListItem> Klassen { get; } = [];
    public ObservableCollection<FehlzeitRowViewModel> Rows { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasKlasse))]
    [NotifyCanExecuteChangedFor(nameof(ImportierenCommand))]
    private KlasseListItem? _selectedKlasse;

    [ObservableProperty] private string _termText = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    private bool _loaded;

    public bool HasKlasse => SelectedKlasse is not null;
    public bool IsEmpty => Loaded && HasKlasse && Rows.Count == 0;

    public override async Task OnNavigatedToAsync()
    {
        var settings = await settingsService.GetAsync();
        _schuljahr = settings.Schuljahr;
        _halbjahr = settings.AktuellesHalbjahr;
        TermText = string.Format(CultureInfo.CurrentCulture, Strings.Fehlzeiten_Term,
            string.IsNullOrWhiteSpace(_schuljahr) ? "—" : _schuljahr,
            _halbjahr == Halbjahr.Erstes ? Strings.Halbjahr_First : Strings.Halbjahr_Second);

        var geladen = await klassenService.GetKlassenAsync();

        Klassen.Clear();
        foreach (var k in geladen)
        {
            Klassen.Add(k);
        }

        _suppressReload = true;
        SelectedKlasse ??= Klassen.FirstOrDefault();
        _suppressReload = false;

        await ReloadRowsAsync();
    }

    partial void OnSelectedKlasseChanged(KlasseListItem? value)
    {
        if (!_suppressReload)
        {
            _ = ReloadRowsAsync();
        }
    }

    private async Task ReloadRowsAsync()
    {
        // Overlapping selection changes: only the newest run writes Rows, and in one step.
        var generation = ++_reloadGeneration;
        var klasse = SelectedKlasse;

        var neu = new List<FehlzeitRowViewModel>();
        if (klasse is not null)
        {
            try
            {
                foreach (var zeile in await fehlzeitenService.GetForKlasseAsync(klasse.Id, _schuljahr, _halbjahr))
                {
                    neu.Add(new FehlzeitRowViewModel(zeile, _schuljahr, _halbjahr, fehlzeitenService, saveStatus));
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Fehlzeiten konnten nicht geladen werden");
                return;
            }
        }

        if (generation != _reloadGeneration)
        {
            return;
        }

        Rows.Clear();
        foreach (var row in neu)
        {
            Rows.Add(row);
        }

        Loaded = true;
        OnPropertyChanged(nameof(IsEmpty));
    }

    private bool CanImportieren() => SelectedKlasse is not null;

    /// <summary>
    /// Opens the shared import wizard on the absence profile: reads a SchILD "Schüler-Abwesenheit"
    /// PDF (or an Excel sheet), matches the rows onto this class, and writes the totals for the term.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanImportieren))]
    private async Task ImportierenAsync()
    {
        var klasse = SelectedKlasse;
        if (klasse is null)
        {
            return;
        }

        var completion = new TaskCompletionSource();
        var wizard = new FehlzeitenImportWizardViewModel(
            import.Parser, import.FehlzeitenCommitter, import.FilePicker, import.Clipboard,
            klasse.Id, klasse.Name, _schuljahr, _halbjahr,
            onFinished: () => completion.TrySetResult());

        var window = new ImportWizardWindow(wizard) { Owner = System.Windows.Application.Current.MainWindow };
        window.Closed += (_, _) => completion.TrySetResult();
        window.Show();

        await completion.Task;
        await ReloadRowsAsync();
    }
}

public sealed partial class FehlzeitRowViewModel : ObservableObject
{
    private readonly Guid _kindId;
    private readonly string _schuljahr;
    private readonly Halbjahr _halbjahr;
    private readonly IFehlzeitenService _service;
    private readonly ISaveStatusService _saveStatus;
    private readonly AutosaveDebouncer _autosave = new();

    public FehlzeitRowViewModel(FehlzeitZeile zeile, string schuljahr, Halbjahr halbjahr,
        IFehlzeitenService service, ISaveStatusService saveStatus)
    {
        _kindId = zeile.KindId;
        _schuljahr = schuljahr;
        _halbjahr = halbjahr;
        _service = service;
        _saveStatus = saveStatus;
        AnzeigeName = zeile.AnzeigeName;
        _gesamt = zeile.StundenGesamt;
        _unentschuldigt = zeile.StundenUnentschuldigt;
        _autosave.Configure(SaveAsync);
    }

    public string AnzeigeName { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IstGueltig), nameof(ErrorText))]
    private int _gesamt;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IstGueltig), nameof(ErrorText))]
    private int _unentschuldigt;

    public bool IstGueltig => Gesamt >= 0 && Unentschuldigt >= 0 && Unentschuldigt <= Gesamt;
    public string ErrorText => IstGueltig ? string.Empty : Strings.Fehlzeiten_Error;

    partial void OnGesamtChanged(int value) => OnEdited();
    partial void OnUnentschuldigtChanged(int value) => OnEdited();

    private void OnEdited()
    {
        if (!IstGueltig)
        {
            _autosave.Cancel();
            return;
        }

        _saveStatus.MarkDirty();
        _autosave.Bump();
    }

    private async Task SaveAsync()
    {
        try
        {
            await _service.SaveAsync(_kindId, _schuljahr, _halbjahr, Gesamt, Unentschuldigt);
            _saveStatus.MarkSaved();
        }
        catch (EntityValidationException)
        {
            // Guarded by IstGueltig already; ignore the race.
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Fehlzeit für {Kind} konnte nicht gespeichert werden", AnzeigeName);
        }
    }
}
