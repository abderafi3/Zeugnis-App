using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using ZeugnisApp.App.Resources;
using ZeugnisApp.App.Services;
using ZeugnisApp.App.ViewModels.Klassen;
using ZeugnisApp.Application.Abstractions;
using ZeugnisApp.Domain.Entities;
using ZeugnisApp.Domain.Enums;

namespace ZeugnisApp.App.ViewModels.Formulare;

public sealed partial class FormulareListViewModel(
    IFormulareService service, IDialogService dialogs, IFormulareNavigator navigator)
    : ObservableObject, IAsyncInitializable
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    private bool _loaded;

    public ObservableCollection<FormularCardViewModel> Formulare { get; } = [];

    public bool IsEmpty => Loaded && Formulare.Count == 0;

    public async Task InitializeAsync()
    {
        Formulare.Clear();
        foreach (var item in await service.GetAllAsync())
        {
            Formulare.Add(new FormularCardViewModel(item, service, dialogs, navigator, ReloadAsync));
        }

        Loaded = true;
        OnPropertyChanged(nameof(IsEmpty));
    }

    private Task ReloadAsync() => InitializeAsync();

    [RelayCommand]
    private async Task NeuesFormularAsync()
    {
        try
        {
            var created = await service.SaveAsync(new Formular
            {
                Anzeigename = "Neues Formular",
                Jahrgang = 1,
                GiltFuerHalbjahr = null,
            });
            navigator.Edit(created.Id);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Neues Formular konnte nicht angelegt werden");
        }
    }
}

public sealed partial class FormularCardViewModel(
    FormularListItem item, IFormulareService service, IDialogService dialogs, IFormulareNavigator navigator, Func<Task> reload)
    : ObservableObject
{
    public Guid Id => item.Id;
    public string Anzeigename => item.Anzeigename;

    public string JahrgangHalbjahr => string.Format(
        CultureInfo.CurrentCulture, Strings.Formulare_JahrgangHalbjahr, item.Jahrgang, HalbjahrText);

    public bool IstBereit => item.IstGenerierungsbereit;
    public string StatusText => item.IstGenerierungsbereit ? Strings.Formulare_StatusReady : Strings.Formulare_StatusMissing;
    public bool IstZieldifferent => item.IstZieldifferent;

    private string HalbjahrText => item.GiltFuerHalbjahr switch
    {
        Halbjahr.Erstes => Strings.Halbjahr_First,
        Halbjahr.Zweites => Strings.Halbjahr_Second,
        _ => Strings.Formulare_HalbjahrBoth,
    };

    [RelayCommand]
    private void Bearbeiten() => navigator.Edit(item.Id);

    [RelayCommand]
    private async Task LoeschenAsync()
    {
        var ok = await dialogs.ConfirmAsync(
            Strings.Formulare_DeleteConfirm_Title,
            string.Format(CultureInfo.CurrentCulture, Strings.Formulare_DeleteConfirm_Body, item.Anzeigename));
        if (!ok)
        {
            return;
        }

        try
        {
            await service.DeleteAsync(item.Id);
            await reload();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Formular {Formular} konnte nicht gelöscht werden", item.Anzeigename);
        }
    }
}
