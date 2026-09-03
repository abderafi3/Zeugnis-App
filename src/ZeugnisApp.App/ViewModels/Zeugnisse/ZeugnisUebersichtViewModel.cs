using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using ZeugnisApp.App.Resources;
using ZeugnisApp.App.ViewModels.Klassen;
using ZeugnisApp.Application.Abstractions;
using ZeugnisApp.Domain.Enums;

namespace ZeugnisApp.App.ViewModels.Zeugnisse;

public sealed partial class ZeugnisUebersichtViewModel(
    IZeugnisService zeugnisse, IKlassenService klassen, ISchoolSettingsService settings, IZeugnisseNavigator navigator)
    : ObservableObject, IAsyncInitializable
{
    private static readonly Guid AutoFormular = Guid.Empty;

    private string _schuljahr = string.Empty;
    private Halbjahr _halbjahr = Halbjahr.Erstes;
    private int _reloadGeneration;
    private bool _suppressReload;

    public ObservableCollection<KlasseListItem> Klassen { get; } = [];
    public ObservableCollection<ZeugnisUebersichtRowViewModel> Rows { get; } = [];

    /// <summary>Templates offered per row; the first entry ("automatisch") clears the manual pin.</summary>
    public ObservableCollection<FormularOption> FormularOptionen { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasKlasse))]
    private KlasseListItem? _selectedKlasse;

    [ObservableProperty] private string _termText = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    private bool _loaded;

    public bool HasKlasse => SelectedKlasse is not null;
    public bool IsEmpty => Loaded && HasKlasse && Rows.Count == 0;

    public async Task InitializeAsync()
    {
        var s = await settings.GetAsync();
        _schuljahr = s.Schuljahr;
        _halbjahr = s.AktuellesHalbjahr;
        TermText = string.Format(CultureInfo.CurrentCulture, Strings.Zeugnisse_Term,
            string.IsNullOrWhiteSpace(_schuljahr) ? "—" : _schuljahr,
            _halbjahr == Halbjahr.Erstes ? Strings.Halbjahr_First : Strings.Halbjahr_Second);

        var geladen = await klassen.GetKlassenAsync();
        var formulare = await klassen.GetFormularOptionenAsync();

        FormularOptionen.Clear();
        FormularOptionen.Add(new FormularOption(AutoFormular, Strings.Zeugnisse_Formular_Auto));
        foreach (var f in formulare)
        {
            FormularOptionen.Add(f);
        }

        // Keep the current selection across a refresh (records compare by value, so match on Id).
        var vorherId = SelectedKlasse?.Id;
        Klassen.Clear();
        foreach (var k in geladen)
        {
            Klassen.Add(k);
        }

        _suppressReload = true;
        SelectedKlasse = (vorherId is { } id ? Klassen.FirstOrDefault(k => k.Id == id) : null)
                         ?? Klassen.FirstOrDefault();
        _suppressReload = false;

        await ReloadAsync();
    }

    partial void OnSelectedKlasseChanged(KlasseListItem? value)
    {
        if (!_suppressReload)
        {
            _ = ReloadAsync();
        }
    }

    private async Task ReloadAsync()
    {
        // Selection changes and the initial load can overlap; only the newest run may
        // touch Rows, and it does so in one shot after its data is ready.
        var generation = ++_reloadGeneration;
        var klasse = SelectedKlasse;

        var neu = new List<ZeugnisUebersichtRowViewModel>();
        if (klasse is not null)
        {
            try
            {
                foreach (var zeile in await zeugnisse.GetUebersichtForKlasseAsync(klasse.Id, _schuljahr, _halbjahr))
                {
                    neu.Add(new ZeugnisUebersichtRowViewModel(
                        zeile, _schuljahr, _halbjahr, zeugnisse, navigator, FormularOptionen, ReloadAsync));
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Zeugnisübersicht konnte nicht geladen werden");
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
}

public sealed partial class ZeugnisUebersichtRowViewModel : ObservableObject
{
    private readonly ZeugnisUebersichtZeile _zeile;
    private readonly string _schuljahr;
    private readonly Halbjahr _halbjahr;
    private readonly IZeugnisService _zeugnisse;
    private readonly IZeugnisseNavigator _navigator;
    private readonly Func<Task> _reload;
    private bool _busy;

    public ZeugnisUebersichtRowViewModel(
        ZeugnisUebersichtZeile zeile, string schuljahr, Halbjahr halbjahr,
        IZeugnisService zeugnisse, IZeugnisseNavigator navigator,
        IReadOnlyList<FormularOption> formularOptionen, Func<Task> reload)
    {
        _zeile = zeile;
        _schuljahr = schuljahr;
        _halbjahr = halbjahr;
        _zeugnisse = zeugnisse;
        _navigator = navigator;
        _reload = reload;
        FormularOptionen = formularOptionen;
        _selectedFormularId = zeile.ManuellesFormularId ?? Guid.Empty;
    }

    public string AnzeigeName => _zeile.AnzeigeName;
    public string FormularName => _zeile.FormularName ?? "—";

    public string StatusText => !_zeile.FormularVerfuegbar ? Strings.Zeugnisse_Status_KeinFormular
        : _zeile.Fertiggestellt ? Strings.Zeugnisse_Status_Fertig
        : Strings.Zeugnisse_Status_Entwurf;

    public bool IstFertig => _zeile.Fertiggestellt;
    public bool KannBearbeiten => _zeile.FormularVerfuegbar;
    public bool KannStatusAendern => _zeile.ZeugnisId is not null;
    public string StatusToggleText => _zeile.Fertiggestellt ? Strings.Zeugnisse_Reopen : Strings.Zeugnisse_MarkDone;

    public IReadOnlyList<FormularOption> FormularOptionen { get; }

    private Guid _selectedFormularId;

    /// <summary>Bound to the per-row template picker. <see cref="Guid.Empty"/> means "auto-suggest".</summary>
    public Guid SelectedFormularId
    {
        get => _selectedFormularId;
        set
        {
            if (_selectedFormularId == value || _busy)
            {
                return;
            }

            _selectedFormularId = value;
            OnPropertyChanged();
            _ = ApplyFormularAsync(value);
        }
    }

    private async Task ApplyFormularAsync(Guid value)
    {
        _busy = true;
        try
        {
            await _zeugnisse.SetManuellesFormularAsync(_zeile.KindId, value == Guid.Empty ? null : value);

            // The reload rebuilds Rows (and this very ComboBox). Let the ComboBox finish its
            // selection change first — clearing the list from inside it tears down a live
            // Selector and throws ("SelectedItems … nur in Mehrfachauswahlmodi").
            if (System.Windows.Application.Current?.Dispatcher is { } dispatcher)
            {
                await dispatcher.InvokeAsync(static () => { }, DispatcherPriority.Background);
            }

            await _reload();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Formular konnte nicht zugewiesen werden");
        }
        finally
        {
            _busy = false;
        }
    }

    [RelayCommand]
    private Task OeffnenAsync() => _navigator.EditZeugnisAsync(_zeile.KindId, _schuljahr, _halbjahr);

    [RelayCommand]
    private async Task StatusUmschaltenAsync()
    {
        if (_zeile.ZeugnisId is not { } id)
        {
            return;
        }

        try
        {
            await _zeugnisse.SetFertiggestelltAsync(id, !_zeile.Fertiggestellt);
            await _reload();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Status konnte nicht geändert werden");
        }
    }
}
