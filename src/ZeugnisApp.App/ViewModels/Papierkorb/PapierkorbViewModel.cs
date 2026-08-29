using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using ZeugnisApp.App.Resources;
using ZeugnisApp.Application.Abstractions;

namespace ZeugnisApp.App.ViewModels.Papierkorb;

/// <summary>The Papierkorb screen (plan §7.2 / §Sprint 11): recently deleted items with restore.</summary>
public sealed partial class PapierkorbViewModel(IPapierkorbService papierkorb, ISchoolSettingsService settings)
    : ViewModelBase
{
    public ObservableCollection<PapierkorbRowViewModel> Eintraege { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    private bool _loaded;

    [ObservableProperty] private string _introText = string.Empty;

    public bool IsEmpty => Loaded && Eintraege.Count == 0;

    public override async Task OnNavigatedToAsync()
    {
        var s = await settings.GetAsync();
        IntroText = string.Format(CultureInfo.CurrentCulture, Strings.Papierkorb_Intro, s.PapierkorbAufbewahrungTage);
        await ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        Eintraege.Clear();
        try
        {
            foreach (var e in await papierkorb.GetEntriesAsync())
            {
                Eintraege.Add(new PapierkorbRowViewModel(e, papierkorb, ReloadAsync));
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Papierkorb konnte nicht geladen werden");
        }

        Loaded = true;
        OnPropertyChanged(nameof(IsEmpty));
    }
}

public sealed partial class PapierkorbRowViewModel(PapierkorbEintrag eintrag, IPapierkorbService service, Func<Task> reload)
    : ObservableObject
{
    public string Typ => eintrag.EntityType;
    public string Bezeichnung => eintrag.Bezeichnung;
    public string DatumText => eintrag.DeletedAtUtc.ToLocalTime().ToString("dd.MM.yyyy HH:mm", CultureInfo.CurrentCulture);

    [RelayCommand]
    private async Task WiederherstellenAsync()
    {
        try
        {
            await service.RestoreAsync(eintrag.AuditEntryId);
            await reload();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Wiederherstellen fehlgeschlagen");
        }
    }
}
