using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using ZeugnisApp.App.Resources;
using ZeugnisApp.App.Services;
using ZeugnisApp.Application;
using ZeugnisApp.Application.Abstractions;
using ZeugnisApp.Domain.Entities;

namespace ZeugnisApp.App.ViewModels.Klassen;

public sealed partial class KlasseEditViewModel : ObservableObject, IAsyncInitializable, IAsyncFlushable
{
    private readonly IKlassenService _service;
    private readonly ISaveStatusService _saveStatus;
    private readonly IKlassenNavigator _navigator;
    private readonly Guid? _klasseId;
    private readonly AutosaveDebouncer _autosave = new();
    private Klasse _klasse = new();
    private bool _loading;

    public KlasseEditViewModel(IKlassenService service, ISaveStatusService saveStatus, IKlassenNavigator navigator, Guid? klasseId)
    {
        _service = service;
        _saveStatus = saveStatus;
        _navigator = navigator;
        _klasseId = klasseId;
        _autosave.Configure(SaveAsync);
    }

    public string Title => _klasseId is null ? Strings.KlasseEdit_TitleNew : Strings.KlasseEdit_TitleEdit;

    public string Name { get => _klasse.Name; set => SetOnEntity(_klasse.Name, value, v => _klasse.Name = v); }
    public int Jahrgang { get => _klasse.Jahrgang; set => SetOnEntity(_klasse.Jahrgang, value, v => _klasse.Jahrgang = v); }
    public string Klassenleitung { get => _klasse.Klassenleitung; set => SetOnEntity(_klasse.Klassenleitung, value, v => _klasse.Klassenleitung = v); }

    public DateTime? Zeugnisdatum
    {
        get => _klasse.Zeugnisdatum is { } d ? d.ToDateTime(TimeOnly.MinValue) : null;
        set => SetOnEntity(_klasse.Zeugnisdatum, value is { } v ? DateOnly.FromDateTime(v) : null, x => _klasse.Zeugnisdatum = x);
    }

    public IReadOnlyList<int> StundenOptionen { get; } = Enumerable.Range(0, 21).ToArray();

    public int Mo { get => _klasse.Wochenstundenplan.Montag; set => SetOnEntity(_klasse.Wochenstundenplan.Montag, value, v => _klasse.Wochenstundenplan.Montag = v); }
    public int Di { get => _klasse.Wochenstundenplan.Dienstag; set => SetOnEntity(_klasse.Wochenstundenplan.Dienstag, value, v => _klasse.Wochenstundenplan.Dienstag = v); }
    public int Mi { get => _klasse.Wochenstundenplan.Mittwoch; set => SetOnEntity(_klasse.Wochenstundenplan.Mittwoch, value, v => _klasse.Wochenstundenplan.Mittwoch = v); }
    public int Do { get => _klasse.Wochenstundenplan.Donnerstag; set => SetOnEntity(_klasse.Wochenstundenplan.Donnerstag, value, v => _klasse.Wochenstundenplan.Donnerstag = v); }
    public int Fr { get => _klasse.Wochenstundenplan.Freitag; set => SetOnEntity(_klasse.Wochenstundenplan.Freitag, value, v => _klasse.Wochenstundenplan.Freitag = v); }

    [ObservableProperty] private string? _nameError;
    [ObservableProperty] private string? _jahrgangError;

    public async Task InitializeAsync()
    {
        _loading = true;
        try
        {
            _klasse = _klasseId is { } id
                ? await _service.GetKlasseAsync(id) ?? new Klasse()
                : new Klasse { Jahrgang = 1 };
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
        _navigator.ShowKlassenListe();
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
            _klasse = await _service.SaveKlasseAsync(_klasse);
            _saveStatus.MarkSaved();
        }
        catch (EntityValidationException ex)
        {
            ApplyServerErrors(ex);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Klasse konnte nicht gespeichert werden");
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
        NameError = string.IsNullOrWhiteSpace(Name) ? Strings.KlasseEdit_Error_NameRequired : null;
        JahrgangError = Jahrgang is < 1 or > 4 ? Strings.KlasseEdit_Error_Jahrgang : null;
        return NameError is null && JahrgangError is null;
    }

    private void ApplyServerErrors(EntityValidationException ex)
    {
        if (ex.Errors.TryGetValue(nameof(Klasse.Name), out var nameErrors))
        {
            NameError = nameErrors.FirstOrDefault();
        }

        if (ex.Errors.TryGetValue(nameof(Klasse.Jahrgang), out var jgErrors))
        {
            JahrgangError = jgErrors.FirstOrDefault();
        }
    }
}
