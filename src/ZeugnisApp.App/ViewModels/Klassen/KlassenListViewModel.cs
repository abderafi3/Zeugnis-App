using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using ZeugnisApp.App.Resources;
using ZeugnisApp.App.Services;
using ZeugnisApp.Application.Abstractions;

namespace ZeugnisApp.App.ViewModels.Klassen;

public sealed partial class KlassenListViewModel(
    IKlassenService service,
    IDialogService dialogs,
    IKlassenNavigator navigator)
    : ObservableObject, IAsyncInitializable
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    private bool _loaded;

    public ObservableCollection<KlasseCardViewModel> Klassen { get; } = [];

    public bool IsEmpty => Loaded && Klassen.Count == 0;

    public async Task InitializeAsync()
    {
        Klassen.Clear();
        foreach (var item in await service.GetKlassenAsync())
        {
            Klassen.Add(new KlasseCardViewModel(item, service, dialogs, navigator, ReloadAsync));
        }

        Loaded = true;
        OnPropertyChanged(nameof(IsEmpty));
    }

    private Task ReloadAsync() => InitializeAsync();

    [RelayCommand]
    private void NeueKlasse() => navigator.EditKlasse(null);
}

public sealed partial class KlasseCardViewModel(
    KlasseListItem item,
    IKlassenService service,
    IDialogService dialogs,
    IKlassenNavigator navigator,
    Func<Task> reload)
    : ObservableObject
{
    public Guid Id => item.Id;
    public string Name => item.Name;
    public string JahrgangBadge => string.Format(CultureInfo.CurrentCulture, Strings.Klassen_JahrgangBadge, item.Jahrgang);
    public string Klassenleitung => item.Klassenleitung;
    public string KinderText => string.Format(CultureInfo.CurrentCulture, Strings.Klassen_ChildCount, item.KinderAnzahl);
    public bool HasIncomplete => item.UnvollstaendigeKinder > 0;
    public string IncompleteText => $"{item.UnvollstaendigeKinder} {Strings.Kinder_Incomplete}";

    [RelayCommand]
    private void Oeffnen() => navigator.ShowKinder(item.Id, item.Name);

    [RelayCommand]
    private void Bearbeiten() => navigator.EditKlasse(item.Id);

    [RelayCommand]
    private async Task LoeschenAsync()
    {
        var confirmed = await dialogs.ConfirmAsync(
            Strings.Klassen_DeleteConfirm_Title,
            string.Format(CultureInfo.CurrentCulture, Strings.Klassen_DeleteConfirm_Body, item.Name));

        if (!confirmed)
        {
            return;
        }

        try
        {
            await service.DeleteKlasseAsync(item.Id);
            await reload();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Klasse {Klasse} konnte nicht gelöscht werden", item.Name);
        }
    }
}
