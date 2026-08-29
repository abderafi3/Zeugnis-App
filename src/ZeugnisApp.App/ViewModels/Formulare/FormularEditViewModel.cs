using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using ZeugnisApp.App.Resources;
using ZeugnisApp.App.Services;
using ZeugnisApp.App.ViewModels.Klassen;
using ZeugnisApp.Application;
using ZeugnisApp.Application.Abstractions;
using ZeugnisApp.Domain.Entities;
using ZeugnisApp.Domain.Enums;

namespace ZeugnisApp.App.ViewModels.Formulare;

public sealed partial class FormularEditViewModel : ObservableObject, IAsyncInitializable, IAsyncFlushable
{
    private readonly IFormulareService _service;
    private readonly ISaveStatusService _saveStatus;
    private readonly IFormulareNavigator _navigator;
    private readonly Guid? _formularId;
    private readonly AutosaveDebouncer _autosave = new();
    private Formular _formular = new();
    private bool _loading;

    public FormularEditViewModel(IFormulareService service, ISaveStatusService saveStatus, IFormulareNavigator navigator, Guid? formularId)
    {
        _service = service;
        _saveStatus = saveStatus;
        _navigator = navigator;
        _formularId = formularId;
        _autosave.Configure(SaveAsync);
        Freitextfelder.CollectionChanged += (_, _) => OnEdited();
    }

    public string Title => _formularId is null ? Strings.FormularEdit_TitleNew : Strings.FormularEdit_TitleEdit;

    public ObservableCollection<FreitextFeldRowViewModel> Freitextfelder { get; } = [];

    public string Anzeigename { get => _formular.Anzeigename; set => Set(_formular.Anzeigename, value, v => _formular.Anzeigename = v); }
    public string Ueberschrift { get => _formular.Ueberschrift ?? string.Empty; set => Set(_formular.Ueberschrift ?? string.Empty, value, v => _formular.Ueberschrift = v); }
    public int Jahrgang { get => _formular.Jahrgang; set => Set(_formular.Jahrgang, value, v => _formular.Jahrgang = v); }

    /// <summary>0 = ganzes Jahr, 1 = 1. Halbjahr, 2 = 2. Halbjahr.</summary>
    public int HalbjahrIndex
    {
        get => _formular.GiltFuerHalbjahr switch { Halbjahr.Erstes => 1, Halbjahr.Zweites => 2, _ => 0 };
        set => Set(HalbjahrIndex, value, v => _formular.GiltFuerHalbjahr = v switch
        {
            1 => Halbjahr.Erstes,
            2 => Halbjahr.Zweites,
            _ => null,
        });
    }

    public bool HatNotenblock { get => _formular.HatNotenblock; set => Set(_formular.HatNotenblock, value, v => _formular.HatNotenblock = v); }
    public bool HatBegruendeteEmpfehlung { get => _formular.HatBegruendeteEmpfehlung; set => Set(_formular.HatBegruendeteEmpfehlung, value, v => _formular.HatBegruendeteEmpfehlung = v); }
    public bool HatVersetzungsvermerk { get => _formular.HatVersetzungsvermerk; set => Set(_formular.HatVersetzungsvermerk, value, v => _formular.HatVersetzungsvermerk = v); }
    public bool IstZieldifferent { get => _formular.IstZieldifferent; set => Set(_formular.IstZieldifferent, value, v => _formular.IstZieldifferent = v); }

    [ObservableProperty] private string _faecherText = string.Empty;
    [ObservableProperty] private string? _anzeigenameError;
    [ObservableProperty] private string? _feldError;

    public string WordFileHint => Strings.FormularEdit_WordFile_Hint;

    partial void OnFaecherTextChanged(string value) => OnEdited();

    public async Task InitializeAsync()
    {
        _loading = true;
        try
        {
            _formular = _formularId is { } id
                ? await _service.GetAsync(id) ?? new Formular()
                : new Formular { Jahrgang = 1 };

            FaecherText = string.Join(Environment.NewLine, _formular.Faecher);

            Freitextfelder.Clear();
            foreach (var feld in _formular.Freitextfelder.OrderBy(f => f.Reihenfolge))
            {
                Freitextfelder.Add(new FreitextFeldRowViewModel(feld, OnEdited, Remove));
            }

            OnPropertyChanged(propertyName: null);
        }
        finally
        {
            _loading = false;
        }
    }

    [RelayCommand]
    private void FeldHinzufuegen()
    {
        Freitextfelder.Add(new FreitextFeldRowViewModel(new FreitextFeldDefinition(), OnEdited, Remove));
    }

    private void Remove(FreitextFeldRowViewModel row)
    {
        Freitextfelder.Remove(row);
    }

    [RelayCommand]
    private async Task ZurueckAsync()
    {
        await FlushAsync();
        _navigator.ShowListe();
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
        _formular.Faecher = FaecherText
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => s.Length > 0)
            .ToList();

        _formular.Freitextfelder = Freitextfelder
            .Select((r, i) => new FreitextFeldDefinition
            {
                Name = r.Name.Trim(),
                Hinweistext = string.IsNullOrWhiteSpace(r.Hinweistext) ? null : r.Hinweistext.Trim(),
                MaxZeichen = r.MaxZeichen,
                Reihenfolge = i,
            })
            .ToList();

        try
        {
            _formular = await _service.SaveAsync(_formular);
            _saveStatus.MarkSaved();
        }
        catch (EntityValidationException ex)
        {
            if (ex.Errors.ContainsKey(nameof(Formular.Anzeigename)))
            {
                AnzeigenameError = Strings.FormularEdit_Error_NameRequired;
            }

            if (ex.Errors.Keys.Any(k => k.Contains("Freitextfelder", StringComparison.Ordinal)))
            {
                FeldError = Strings.FormularEdit_Error_FeldNamesUnique;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Formular konnte nicht gespeichert werden");
        }
    }

    private void OnEdited()
    {
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

    private void Set<T>(T current, T value, Action<T> assign, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(current, value))
        {
            return;
        }

        assign(value);
        OnPropertyChanged(propertyName);
        OnEdited();
    }

    private bool Validate()
    {
        AnzeigenameError = string.IsNullOrWhiteSpace(Anzeigename) ? Strings.FormularEdit_Error_NameRequired : null;

        var names = Freitextfelder.Select(f => f.Name.Trim().ToLowerInvariant()).ToList();
        var duplicatesOrEmpty = names.Any(string.IsNullOrEmpty) || names.Distinct().Count() != names.Count;
        FeldError = duplicatesOrEmpty && Freitextfelder.Count > 0 ? Strings.FormularEdit_Error_FeldNamesUnique : null;

        return AnzeigenameError is null && FeldError is null;
    }
}

public sealed partial class FreitextFeldRowViewModel : ObservableObject
{
    private readonly FreitextFeldDefinition _feld;
    private readonly Action _onEdited;
    private readonly Action<FreitextFeldRowViewModel> _remove;

    public FreitextFeldRowViewModel(FreitextFeldDefinition feld, Action onEdited, Action<FreitextFeldRowViewModel> remove)
    {
        _feld = feld;
        _onEdited = onEdited;
        _remove = remove;
        _name = feld.Name;
        _hinweistext = feld.Hinweistext ?? string.Empty;
        _maxZeichen = feld.MaxZeichen;
    }

    [ObservableProperty] private string _name;
    [ObservableProperty] private string _hinweistext;
    [ObservableProperty] private int? _maxZeichen;

    partial void OnNameChanged(string value) => _onEdited();
    partial void OnHinweistextChanged(string value) => _onEdited();
    partial void OnMaxZeichenChanged(int? value) => _onEdited();

    [RelayCommand]
    private void Entfernen() => _remove(this);
}
