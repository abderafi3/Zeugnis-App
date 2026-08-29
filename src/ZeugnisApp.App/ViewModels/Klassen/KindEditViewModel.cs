using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using ZeugnisApp.App.Resources;
using ZeugnisApp.App.Services;
using ZeugnisApp.Application;
using ZeugnisApp.Application.Abstractions;
using ZeugnisApp.Domain.Entities;
using ZeugnisApp.Domain.Enums;

namespace ZeugnisApp.App.ViewModels.Klassen;

public sealed partial class KindEditViewModel : ObservableObject, IAsyncInitializable, IAsyncFlushable
{
    private static readonly Guid NoFormular = Guid.Empty;

    private readonly IKlassenService _service;
    private readonly ISaveStatusService _saveStatus;
    private readonly IKlassenNavigator _navigator;
    private readonly Guid _klasseId;
    private readonly string _klasseName;
    private readonly Guid? _kindId;
    private readonly AutosaveDebouncer _autosave = new();
    private Kind _kind = new();
    private bool _loading;

    public KindEditViewModel(IKlassenService service, ISaveStatusService saveStatus, IKlassenNavigator navigator, Guid klasseId, string klasseName, Guid? kindId)
    {
        _service = service;
        _saveStatus = saveStatus;
        _navigator = navigator;
        _klasseId = klasseId;
        _klasseName = klasseName;
        _kindId = kindId;
        _autosave.Configure(SaveAsync);
    }

    public string Title => _kindId is null ? Strings.KindEdit_TitleNew : Strings.KindEdit_TitleEdit;

    public IReadOnlyList<EnumOption<Geschlecht?>> GeschlechtOptionen => EnumText.GeschlechtOptions;
    public IReadOnlyList<EnumOption<Foerderstatus>> FoerderstatusOptionen => EnumText.FoerderstatusOptions;
    public ObservableCollection<FormularOption> FormularOptionen { get; } = [];

    public string Vorname { get => _kind.Vorname; set => SetOnEntity(_kind.Vorname, value, v => _kind.Vorname = v); }
    public string Nachname { get => _kind.Nachname; set => SetOnEntity(_kind.Nachname, value, v => _kind.Nachname = v); }

    public DateTime? Geburtsdatum
    {
        get => _kind.Geburtsdatum is { } d ? d.ToDateTime(TimeOnly.MinValue) : null;
        set => SetOnEntity(_kind.Geburtsdatum, value is { } v ? DateOnly.FromDateTime(v) : null, x => _kind.Geburtsdatum = x);
    }

    public Geschlecht? Geschlecht { get => _kind.Geschlecht; set => SetOnEntity(_kind.Geschlecht, value, v => _kind.Geschlecht = v); }

    public Foerderstatus Foerderstatus
    {
        get => _kind.Foerderstatus;
        set
        {
            SetOnEntity(_kind.Foerderstatus, value, v => _kind.Foerderstatus = v);
            OnPropertyChanged(nameof(ShowFoerderschwerpunkt));
        }
    }

    public bool ShowFoerderschwerpunkt => _kind.Foerderstatus != Foerderstatus.Regel;

    public string? Foerderschwerpunkt { get => _kind.Foerderschwerpunkt; set => SetOnEntity(_kind.Foerderschwerpunkt, value, v => _kind.Foerderschwerpunkt = v); }

    public Guid SelectedFormularId
    {
        get => _kind.ManuellesFormularId ?? NoFormular;
        set => SetOnEntity(_kind.ManuellesFormularId, value == NoFormular ? (Guid?)null : value, x => _kind.ManuellesFormularId = x);
    }

    [ObservableProperty] private string? _vornameError;
    [ObservableProperty] private string? _nachnameError;

    public async Task InitializeAsync()
    {
        _loading = true;
        try
        {
            _kind = _kindId is { } id
                ? await _service.GetKindAsync(id) ?? new Kind { KlasseId = _klasseId }
                : new Kind { KlasseId = _klasseId };
            _kind.KlasseId = _klasseId;

            FormularOptionen.Clear();
            FormularOptionen.Add(new FormularOption(NoFormular, Strings.KindEdit_ManuellesFormular_None));
            foreach (var option in await _service.GetFormularOptionenAsync())
            {
                FormularOptionen.Add(option);
            }

            OnPropertyChanged(propertyName: null);
        }
        finally
        {
            _loading = false;
        }
    }

    [RelayCommand]
    private async Task ZurueckAsync()
    {
        await FlushAsync();
        _navigator.ShowKinder(_klasseId, _klasseName);
    }

    public async Task FlushAsync()
    {
        _autosave.Cancel();
        if (_loading || !Validate())
        {
            return;
        }

        await SaveAsync();
    }

    private async Task SaveAsync()
    {
        try
        {
            _kind = await _service.SaveKindAsync(_kind);
            _saveStatus.MarkSaved();
        }
        catch (EntityValidationException ex)
        {
            if (ex.Errors.TryGetValue(nameof(Kind.Vorname), out var v))
            {
                VornameError = v.FirstOrDefault();
            }

            if (ex.Errors.TryGetValue(nameof(Kind.Nachname), out var n))
            {
                NachnameError = n.FirstOrDefault();
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Kind konnte nicht gespeichert werden");
        }
    }

    private void SetOnEntity<T>(T current, T value, Action<T> assign, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(current, value))
        {
            return;
        }

        assign(value);
        OnPropertyChanged(propertyName);

        if (_loading)
        {
            return;
        }

        if (Validate())
        {
            _saveStatus.MarkDirty();
            _autosave.Bump();
        }
    }

    private bool Validate()
    {
        VornameError = string.IsNullOrWhiteSpace(Vorname) ? Strings.KindEdit_Error_VornameRequired : null;
        NachnameError = string.IsNullOrWhiteSpace(Nachname) ? Strings.KindEdit_Error_NachnameRequired : null;
        return VornameError is null && NachnameError is null;
    }
}
