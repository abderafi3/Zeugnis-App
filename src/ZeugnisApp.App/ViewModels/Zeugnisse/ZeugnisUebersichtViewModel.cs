using System.Collections.ObjectModel;
using System.Globalization;
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
    private string _schuljahr = string.Empty;
    private Halbjahr _halbjahr = Halbjahr.Erstes;

    public ObservableCollection<KlasseListItem> Klassen { get; } = [];
    public ObservableCollection<ZeugnisUebersichtRowViewModel> Rows { get; } = [];

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

        Klassen.Clear();
        foreach (var k in await klassen.GetKlassenAsync())
        {
            Klassen.Add(k);
        }

        SelectedKlasse ??= Klassen.FirstOrDefault();
        await ReloadAsync();
    }

    partial void OnSelectedKlasseChanged(KlasseListItem? value) => _ = ReloadAsync();

    private async Task ReloadAsync()
    {
        Rows.Clear();
        if (SelectedKlasse is { } klasse)
        {
            try
            {
                foreach (var zeile in await zeugnisse.GetUebersichtForKlasseAsync(klasse.Id, _schuljahr, _halbjahr))
                {
                    Rows.Add(new ZeugnisUebersichtRowViewModel(zeile, _schuljahr, _halbjahr, zeugnisse, navigator, ReloadAsync));
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Zeugnisübersicht konnte nicht geladen werden");
            }
        }

        Loaded = true;
        OnPropertyChanged(nameof(IsEmpty));
    }
}

public sealed partial class ZeugnisUebersichtRowViewModel(
    ZeugnisUebersichtZeile zeile, string schuljahr, Halbjahr halbjahr,
    IZeugnisService zeugnisse, IZeugnisseNavigator navigator, Func<Task> reload)
    : ObservableObject
{
    public string AnzeigeName => zeile.AnzeigeName;
    public string FormularName => zeile.FormularName ?? "—";

    public string StatusText => !zeile.FormularVerfuegbar ? Strings.Zeugnisse_Status_KeinFormular
        : zeile.Fertiggestellt ? Strings.Zeugnisse_Status_Fertig
        : Strings.Zeugnisse_Status_Entwurf;

    public bool IstFertig => zeile.Fertiggestellt;
    public bool KannBearbeiten => zeile.FormularVerfuegbar;
    public bool KannStatusAendern => zeile.ZeugnisId is not null;
    public string StatusToggleText => zeile.Fertiggestellt ? Strings.Zeugnisse_Reopen : Strings.Zeugnisse_MarkDone;

    [RelayCommand]
    private Task OeffnenAsync() => navigator.EditZeugnisAsync(zeile.KindId, schuljahr, halbjahr);

    [RelayCommand]
    private async Task StatusUmschaltenAsync()
    {
        if (zeile.ZeugnisId is not { } id)
        {
            return;
        }

        try
        {
            await zeugnisse.SetFertiggestelltAsync(id, !zeile.Fertiggestellt);
            await reload();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Status konnte nicht geändert werden");
        }
    }
}
