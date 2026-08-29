using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using ZeugnisApp.App.Resources;
using ZeugnisApp.App.Services;
using ZeugnisApp.App.ViewModels.Klassen;
using ZeugnisApp.Application.Abstractions;
using ZeugnisApp.Application.Zeugnisse;
using ZeugnisApp.Domain.Entities;

namespace ZeugnisApp.App.ViewModels.Zeugnisse;

/// <summary>
/// The dynamic report-card editor (plan §Sprint 8): the sections shown depend on the Formular's
/// switches; integrates the Floskel-picker and the transferred Fehlstunden; auto-inserts the
/// mandatory zieldifferent remark. Autosaves. Generation into a .docx is wired in Sprint 9.
/// </summary>
public sealed partial class ZeugnisEditorViewModel : ObservableObject, IAsyncInitializable, IAsyncFlushable
{
    private readonly Zeugnis _zeugnis;
    private readonly Formular _formular;
    private readonly Kind _kind;
    private readonly IZeugnisService _service;
    private readonly IFloskelPickerService _picker;
    private readonly ISaveStatusService _saveStatus;
    private readonly IZeugnisseNavigator _navigator;
    private readonly AutosaveDebouncer _autosave = new();
    private bool _loading;

    public ZeugnisEditorViewModel(
        Zeugnis zeugnis, IZeugnisService service, IFloskelPickerService picker,
        ISaveStatusService saveStatus, IZeugnisseNavigator navigator)
    {
        _zeugnis = zeugnis;
        _formular = zeugnis.Formular ?? new Formular();
        _kind = zeugnis.Kind ?? new Kind();
        _service = service;
        _picker = picker;
        _saveStatus = saveStatus;
        _navigator = navigator;
        _autosave.Configure(SaveAsync);
    }

    public string Title => string.Format(CultureInfo.CurrentCulture, Strings.ZeugnisEditor_Title, _kind.AnzeigeName);
    public string FormularLabel => string.Format(CultureInfo.CurrentCulture, Strings.ZeugnisEditor_FormularLabel, _formular.Anzeigename);

    public ObservableCollection<FreitextEditorViewModel> Freitexte { get; } = [];
    public ObservableCollection<NoteEditorViewModel> Noten { get; } = [];

    public bool ShowNoten => _formular.HatNotenblock;
    public bool ShowEmpfehlung => _formular.HatBegruendeteEmpfehlung;
    public bool ShowVersetzung => _formular.HatVersetzungsvermerk;

    public IReadOnlyList<string> SchulformOptionen => ZeugnisDefaults.Schulformempfehlungen;
    public IReadOnlyList<string> VersetzungsvermerkOptionen => ZeugnisDefaults.Versetzungsvermerke;

    public string? Empfehlung { get => _zeugnis.Empfehlung; set => SetScalar(_zeugnis.Empfehlung, value, v => _zeugnis.Empfehlung = v); }
    public string? EmpfehlungBegruendung { get => _zeugnis.EmpfehlungBegruendung; set => SetScalar(_zeugnis.EmpfehlungBegruendung, value, v => _zeugnis.EmpfehlungBegruendung = v); }
    public string? VersetzungsvermerkText { get => _zeugnis.VersetzungsvermerkText; set => SetScalar(_zeugnis.VersetzungsvermerkText, value, v => _zeugnis.VersetzungsvermerkText = v); }
    public string? Bemerkungen { get => _zeugnis.Bemerkungen; set => SetScalar(_zeugnis.Bemerkungen, value, v => _zeugnis.Bemerkungen = v); }

    public int? FehlstundenGesamt { get => _zeugnis.FehlstundenGesamt; set => SetScalar(_zeugnis.FehlstundenGesamt, value, v => _zeugnis.FehlstundenGesamt = v); }
    public int? FehlstundenUnentschuldigt { get => _zeugnis.FehlstundenUnentschuldigt; set => SetScalar(_zeugnis.FehlstundenUnentschuldigt, value, v => _zeugnis.FehlstundenUnentschuldigt = v); }

    public bool Fertiggestellt
    {
        get => _zeugnis.Fertiggestellt;
        set => SetScalar(_zeugnis.Fertiggestellt, value, v => _zeugnis.Fertiggestellt = v);
    }

    [ObservableProperty] private bool _zieldifferentHinweisEingefuegt;

    public Task InitializeAsync()
    {
        _loading = true;
        try
        {
            // One editor per template free-text field; find-or-create the matching Zeugnis entry.
            Freitexte.Clear();
            foreach (var feld in _formular.Freitextfelder.OrderBy(f => f.Reihenfolge))
            {
                var eintrag = _zeugnis.Freitexte.FirstOrDefault(e => e.FeldName == feld.Name);
                if (eintrag is null)
                {
                    eintrag = new FreitextEintrag { FeldName = feld.Name, Text = string.Empty };
                    _zeugnis.Freitexte.Add(eintrag);
                }

                Freitexte.Add(new FreitextEditorViewModel(eintrag, feld, _picker, _kind, OnEdited));
            }

            // One grade row per template subject.
            Noten.Clear();
            if (_formular.HatNotenblock)
            {
                foreach (var fach in _formular.Faecher)
                {
                    var eintrag = _zeugnis.Noten.FirstOrDefault(n => n.Fach == fach);
                    if (eintrag is null)
                    {
                        eintrag = new NoteEintrag { Fach = fach, Note = null };
                        _zeugnis.Noten.Add(eintrag);
                    }

                    Noten.Add(new NoteEditorViewModel(eintrag, ZeugnisDefaults.Noten, OnEdited));
                }
            }

            // Mandatory zieldifferent remark (plan §Sprint 8).
            var hinweis = ZieldifferentHinweis.For(_kind.Foerderstatus);
            if (hinweis is not null && (_zeugnis.Bemerkungen is null || !_zeugnis.Bemerkungen.Contains(hinweis, StringComparison.Ordinal)))
            {
                _zeugnis.Bemerkungen = string.IsNullOrWhiteSpace(_zeugnis.Bemerkungen)
                    ? hinweis
                    : hinweis + "\n\n" + _zeugnis.Bemerkungen;
                ZieldifferentHinweisEingefuegt = true;
            }

            OnPropertyChanged(propertyName: null);
        }
        finally
        {
            _loading = false;
        }

        // Persist the scaffolded Freitext/Noten entries and any auto-inserted zieldifferent remark.
        return FlushAsync();
    }

    [RelayCommand]
    private async Task ZurueckAsync()
    {
        await FlushAsync();
        _navigator.ShowUebersicht();
    }

    public async Task FlushAsync()
    {
        _autosave.Cancel();
        if (_loading)
        {
            return;
        }

        await SaveAsync();
    }

    private async Task SaveAsync()
    {
        try
        {
            await _service.SaveAsync(_zeugnis);
            _saveStatus.MarkSaved();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Zeugnis konnte nicht gespeichert werden");
        }
    }

    private void OnEdited()
    {
        if (_loading)
        {
            return;
        }

        _saveStatus.MarkDirty();
        _autosave.Bump();
    }

    private void SetScalar<T>(T current, T value, Action<T> assign,
        [System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(current, value))
        {
            return;
        }

        assign(value);
        OnPropertyChanged(propertyName);
        OnEdited();
    }
}

/// <summary>One free-text field: char counter, overflow flag, and a Floskel-picker button.</summary>
public sealed partial class FreitextEditorViewModel : ObservableObject
{
    private readonly FreitextEintrag _eintrag;
    private readonly FreitextFeldDefinition _feld;
    private readonly IFloskelPickerService _picker;
    private readonly Kind _kind;
    private readonly Action _onEdited;

    public FreitextEditorViewModel(FreitextEintrag eintrag, FreitextFeldDefinition feld,
        IFloskelPickerService picker, Kind kind, Action onEdited)
    {
        _eintrag = eintrag;
        _feld = feld;
        _picker = picker;
        _kind = kind;
        _onEdited = onEdited;
    }

    public string Label => _feld.Name;
    public string? Hinweis => _feld.Hinweistext;
    public int? MaxZeichen => _feld.MaxZeichen;

    public string Text
    {
        get => _eintrag.Text;
        set
        {
            if (_eintrag.Text == value)
            {
                return;
            }

            _eintrag.Text = value ?? string.Empty;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ZeichenText));
            OnPropertyChanged(nameof(IstUeberlauf));
            _onEdited();
        }
    }

    public bool IstUeberlauf => MaxZeichen is { } max && max > 0 && _eintrag.Text.Trim().Length > max;

    public string ZeichenText => MaxZeichen is { } max && max > 0
        ? string.Format(CultureInfo.CurrentCulture, Strings.ZeugnisEditor_CharCount, _eintrag.Text.Trim().Length, max)
        : string.Format(CultureInfo.CurrentCulture, Strings.ZeugnisEditor_CharCountNoLimit, _eintrag.Text.Trim().Length);

    [RelayCommand]
    private async Task FloskelEinfuegenAsync()
    {
        var text = await _picker.PickAsync(_kind.Vorname, _kind.Geschlecht);
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        Text = string.IsNullOrWhiteSpace(Text) ? text : Text.TrimEnd() + " " + text;
    }
}

/// <summary>One subject grade dropdown.</summary>
public sealed partial class NoteEditorViewModel(NoteEintrag eintrag, IReadOnlyList<string> optionen, Action onEdited)
    : ObservableObject
{
    public string Fach => eintrag.Fach;
    public IReadOnlyList<string> Optionen => optionen;

    public string? Note
    {
        get => eintrag.Note;
        set
        {
            if (eintrag.Note == value)
            {
                return;
            }

            eintrag.Note = value;
            OnPropertyChanged();
            onEdited();
        }
    }
}
