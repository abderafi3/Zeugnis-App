using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using ZeugnisApp.App.Resources;
using ZeugnisApp.App.Services;
using ZeugnisApp.Application.Abstractions;
using ZeugnisApp.Domain.Entities;

namespace ZeugnisApp.App.ViewModels.Klassen;

public sealed partial class KinderListViewModel(
    IKlassenService service,
    IDialogService dialogs,
    IKlassenNavigator navigator,
    Guid klasseId,
    string klasseName)
    : ObservableObject, IAsyncInitializable
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    private bool _loaded;

    public string Title => string.Format(CultureInfo.CurrentCulture, Strings.Kinder_Title, klasseName);

    public ObservableCollection<KindRowViewModel> Kinder { get; } = [];

    public bool IsEmpty => Loaded && Kinder.Count == 0;

    public async Task InitializeAsync()
    {
        Kinder.Clear();
        foreach (var kind in await service.GetKinderAsync(klasseId))
        {
            Kinder.Add(new KindRowViewModel(kind, service, dialogs, navigator, klasseId, klasseName, ReloadAsync));
        }

        Loaded = true;
        OnPropertyChanged(nameof(IsEmpty));
    }

    private Task ReloadAsync() => InitializeAsync();

    [RelayCommand]
    private void NeuesKind() => navigator.EditKind(klasseId, klasseName, null);

    [RelayCommand]
    private void Importieren() => navigator.ImportKinder(klasseId, klasseName);

    [RelayCommand]
    private void Zurueck() => navigator.ShowKlassenListe();
}

public sealed partial class KindRowViewModel(
    Kind kind,
    IKlassenService service,
    IDialogService dialogs,
    IKlassenNavigator navigator,
    Guid klasseId,
    string klasseName,
    Func<Task> reload)
    : ObservableObject
{
    public Guid Id => kind.Id;
    public string AnzeigeName => kind.AnzeigeName;
    public string GeburtsdatumText => kind.Geburtsdatum?.ToString("dd.MM.yyyy", CultureInfo.CurrentCulture) ?? "—";
    public string GeschlechtText => EnumText.For(kind.Geschlecht);
    public string FoerderstatusText => EnumText.For(kind.Foerderstatus);
    public bool IstVollstaendig => kind.IstVollstaendig;
    public string StatusText => kind.IstVollstaendig ? Strings.Kinder_Complete : Strings.Kinder_Incomplete;

    [RelayCommand]
    private void Bearbeiten() => navigator.EditKind(klasseId, klasseName, kind.Id);

    [RelayCommand]
    private async Task LoeschenAsync()
    {
        var confirmed = await dialogs.ConfirmAsync(
            Strings.Kinder_DeleteConfirm_Title,
            string.Format(CultureInfo.CurrentCulture, Strings.Kinder_DeleteConfirm_Body, kind.AnzeigeName));

        if (!confirmed)
        {
            return;
        }

        try
        {
            await service.DeleteKindAsync(kind.Id);
            await reload();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Kind {Kind} konnte nicht gelöscht werden", kind.AnzeigeName);
        }
    }
}
