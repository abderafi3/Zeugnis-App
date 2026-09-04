using System.Collections.ObjectModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using ZeugnisApp.App.Resources;
using ZeugnisApp.App.Services;
using ZeugnisApp.App.ViewModels.Klassen;
using ZeugnisApp.Application.Abstractions;
using ZeugnisApp.Application.Zeugnisse;
using ZeugnisApp.Domain.Entities;
using ZeugnisApp.Domain.Enums;

namespace ZeugnisApp.App.ViewModels.Zeugnisse;

/// <summary>
/// Lern- und Förderempfehlungen for one child in one term (the per-subject supplement to the
/// report card). One card per subject, each with the three sections from the school's form;
/// autosaves like the other edit screens.
/// </summary>
public sealed partial class FoerderempfehlungenViewModel : ObservableObject, IAsyncInitializable, IAsyncFlushable
{
    private readonly IFoerderempfehlungService _service;
    private readonly ISaveStatusService _saveStatus;
    private readonly IDialogService _dialogs;
    private readonly IZeugnisseNavigator _navigator;
    private readonly Guid _kindId;
    private readonly string _kindName;
    private readonly string _schuljahr;
    private readonly Halbjahr _halbjahr;

    public FoerderempfehlungenViewModel(
        IFoerderempfehlungService service, ISaveStatusService saveStatus, IDialogService dialogs,
        IZeugnisseNavigator navigator, Guid kindId, string kindName, string schuljahr, Halbjahr halbjahr)
    {
        _service = service;
        _saveStatus = saveStatus;
        _dialogs = dialogs;
        _navigator = navigator;
        _kindId = kindId;
        _kindName = kindName;
        _schuljahr = schuljahr;
        _halbjahr = halbjahr;
    }

    public string Titel => string.Format(CultureInfo.CurrentCulture, Strings.Foerderempfehlungen_Title, _kindName);

    public string Untertitel => string.Format(
        CultureInfo.CurrentCulture, Strings.Foerderempfehlungen_Subtitle,
        _schuljahr,
        _halbjahr == Halbjahr.Erstes ? Strings.Halbjahr_First : Strings.Halbjahr_Second);

    public ObservableCollection<FoerderempfehlungRowViewModel> Empfehlungen { get; } = [];

    /// <summary>Subjects that don't yet have a recommendation this term — the "add" picklist.</summary>
    public ObservableCollection<string> FachOptionen { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(HinzufuegenCommand))]
    private string? _neuesFach;

    public bool IstLeer => Empfehlungen.Count == 0;

    public async Task InitializeAsync()
    {
        Empfehlungen.Clear();
        try
        {
            foreach (var eintrag in await _service.GetForKindAsync(_kindId, _schuljahr, _halbjahr))
            {
                Empfehlungen.Add(NewRow(eintrag));
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Lern- und Förderempfehlungen konnten nicht geladen werden");
        }

        RefreshFachOptionen();
        OnPropertyChanged(nameof(IstLeer));
    }

    private bool CanHinzufuegen() => !string.IsNullOrWhiteSpace(NeuesFach);

    [RelayCommand(CanExecute = nameof(CanHinzufuegen))]
    private async Task HinzufuegenAsync()
    {
        var fach = NeuesFach;
        if (string.IsNullOrWhiteSpace(fach))
        {
            return;
        }

        try
        {
            var eintrag = await _service.GetOrCreateAsync(_kindId, _schuljahr, _halbjahr, fach);
            if (Empfehlungen.All(r => r.Id != eintrag.Id))
            {
                Empfehlungen.Add(NewRow(eintrag));
            }

            NeuesFach = null;
            RefreshFachOptionen();
            OnPropertyChanged(nameof(IstLeer));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Förderempfehlung für {Fach} konnte nicht angelegt werden", fach);
        }
    }

    [RelayCommand]
    private async Task ZurueckAsync()
    {
        await FlushAsync();
        _navigator.ShowUebersicht();
    }

    public Task FlushAsync() => Task.WhenAll(Empfehlungen.Select(r => r.FlushAsync()));

    private FoerderempfehlungRowViewModel NewRow(Foerderempfehlung eintrag)
        => new(eintrag, _service, _saveStatus, _dialogs, OnRowDeleted);

    private void OnRowDeleted(Guid id)
    {
        var row = Empfehlungen.FirstOrDefault(r => r.Id == id);
        if (row is not null)
        {
            Empfehlungen.Remove(row);
        }

        RefreshFachOptionen();
        OnPropertyChanged(nameof(IstLeer));
    }

    private void RefreshFachOptionen()
    {
        var vergeben = Empfehlungen.Select(r => r.Fach).ToHashSet(StringComparer.OrdinalIgnoreCase);
        FachOptionen.Clear();
        foreach (var fach in ZeugnisDefaults.Schulfaecher.Where(f => !vergeben.Contains(f)))
        {
            FachOptionen.Add(fach);
        }
    }
}

/// <summary>One subject's recommendation card. Inline edits autosave.</summary>
public sealed partial class FoerderempfehlungRowViewModel : ObservableObject, IAsyncFlushable
{
    private readonly Foerderempfehlung _eintrag;
    private readonly IFoerderempfehlungService _service;
    private readonly ISaveStatusService _saveStatus;
    private readonly IDialogService _dialogs;
    private readonly Action<Guid> _onDeleted;
    private readonly AutosaveDebouncer _autosave = new();

    public FoerderempfehlungRowViewModel(
        Foerderempfehlung eintrag, IFoerderempfehlungService service, ISaveStatusService saveStatus,
        IDialogService dialogs, Action<Guid> onDeleted)
    {
        _eintrag = eintrag;
        _service = service;
        _saveStatus = saveStatus;
        _dialogs = dialogs;
        _onDeleted = onDeleted;
        _autosave.Configure(SaveAsync);
    }

    public Guid Id => _eintrag.Id;
    public string Fach => _eintrag.Fach;

    public string? Beobachtungen
    {
        get => _eintrag.Beobachtungen;
        set => SetField(_eintrag.Beobachtungen, value, v => _eintrag.Beobachtungen = v);
    }

    public string? SchulischeMassnahmen
    {
        get => _eintrag.SchulischeMassnahmen;
        set => SetField(_eintrag.SchulischeMassnahmen, value, v => _eintrag.SchulischeMassnahmen = v);
    }

    public string? EmpfehlungenFuerZuhause
    {
        get => _eintrag.EmpfehlungenFuerZuhause;
        set => SetField(_eintrag.EmpfehlungenFuerZuhause, value, v => _eintrag.EmpfehlungenFuerZuhause = v);
    }

    public bool Fertiggestellt
    {
        get => _eintrag.Fertiggestellt;
        set => SetField(_eintrag.Fertiggestellt, value, v => _eintrag.Fertiggestellt = v);
    }

    [RelayCommand]
    private async Task LoeschenAsync()
    {
        var ok = await _dialogs.ConfirmAsync(
            Strings.Foerderempfehlungen_DeleteConfirm_Title, Strings.Foerderempfehlungen_DeleteConfirm_Body);
        if (!ok)
        {
            return;
        }

        _autosave.Cancel();
        try
        {
            await _service.DeleteAsync(_eintrag.Id);
            _onDeleted(_eintrag.Id);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Förderempfehlung konnte nicht gelöscht werden");
        }
    }

    public async Task FlushAsync()
    {
        _autosave.Cancel();
        await SaveAsync();
    }

    private async Task SaveAsync()
    {
        try
        {
            await _service.SaveAsync(_eintrag);
            _saveStatus.MarkSaved();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Förderempfehlung konnte nicht gespeichert werden");
        }
    }

    private void SetField<T>(T current, T value, Action<T> assign, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(current, value))
        {
            return;
        }

        assign(value);
        OnPropertyChanged(propertyName);
        _saveStatus.MarkDirty();
        _autosave.Bump();
    }
}
