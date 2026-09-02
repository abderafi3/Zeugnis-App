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
using ZeugnisApp.Domain.Enums;
using ZeugnisApp.Domain.Floskeln;
using WpfFilePicker = ZeugnisApp.App.Services.FilePickerService;

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
    private readonly IFloskelnService _floskeln;
    private readonly IFloskelPickerService _picker;
    private readonly ISaveStatusService _saveStatus;
    private readonly IZeugnisseNavigator _navigator;
    private readonly ReportGenerationServices _reporting;
    private readonly AutosaveDebouncer _autosave = new();
    private bool _loading;

    public ZeugnisEditorViewModel(
        Zeugnis zeugnis, IZeugnisService service, IFloskelnService floskeln, IFloskelPickerService picker,
        ISaveStatusService saveStatus, IZeugnisseNavigator navigator, ReportGenerationServices reporting)
    {
        _zeugnis = zeugnis;
        _formular = zeugnis.Formular ?? new Formular();
        _kind = zeugnis.Kind ?? new Kind();
        _service = service;
        _floskeln = floskeln;
        _picker = picker;
        _saveStatus = saveStatus;
        _navigator = navigator;
        _reporting = reporting;
        _autosave.Configure(SaveAsync);
    }

    public string Title => string.Format(CultureInfo.CurrentCulture, Strings.ZeugnisEditor_Title, _kind.AnzeigeName);
    public string FormularLabel => string.Format(CultureInfo.CurrentCulture, Strings.ZeugnisEditor_FormularLabel, _formular.Anzeigename);

    public ObservableCollection<FreitextEditorViewModel> Freitexte { get; } = [];
    public ObservableCollection<NoteEditorViewModel> Noten { get; } = [];

    /// <summary>True once any field has "häufig genutzt" phrases — drives the right-hand sidebar.</summary>
    public bool HatVorschlaege => Freitexte.Any(f => f.HatVorschlaege);

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

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ErzeugenCommand))]
    private bool _erzeugtLaeuft;

    [ObservableProperty] private string? _erzeugenStatus;

    public bool KannErzeugen => _formular.IstGenerierungsbereit;

    public string? ZuletztErzeugt => string.IsNullOrWhiteSpace(_zeugnis.GeneratedFilePath)
        ? null
        : string.Format(CultureInfo.CurrentCulture, Strings.ZeugnisEditor_LastGenerated,
            System.IO.Path.GetFileName(_zeugnis.GeneratedFilePath));

    public async Task InitializeAsync()
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

                Freitexte.Add(new FreitextEditorViewModel(eintrag, feld, _picker, _floskeln, _kind, _formular.Jahrgang, OnEdited));
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
        await FlushAsync();

        // Fill the "häufig genutzt" shortcut list shown in the sidebar.
        await Task.WhenAll(Freitexte.Select(f => f.LadeVorschlaegeAsync()));
        OnPropertyChanged(nameof(HatVorschlaege));
    }

    [RelayCommand]
    private async Task ZurueckAsync()
    {
        await FlushAsync();
        _navigator.ShowUebersicht();
    }

    [RelayCommand]
    private async Task FoerderempfehlungOeffnenAsync()
    {
        await FlushAsync();
        await _navigator.ShowFoerderempfehlungenAsync(
            _kind.Id, _kind.AnzeigeName, _zeugnis.Schuljahr, _zeugnis.Halbjahr);
    }

    [RelayCommand]
    private async Task BemerkungFloskelEinfuegenAsync()
    {
        var text = await _picker.PickAsync(_kind.Vorname, _kind.Geschlecht);
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        Bemerkungen = string.IsNullOrWhiteSpace(Bemerkungen) ? text : Bemerkungen!.TrimEnd() + " " + text;
    }

    private bool CanErzeugen() => KannErzeugen && !ErzeugtLaeuft;

    [RelayCommand(CanExecute = nameof(CanErzeugen))]
    private async Task ErzeugenAsync()
    {
        ErzeugenStatus = null;

        if (!_formular.IstGenerierungsbereit)
        {
            ErzeugenStatus = Strings.ZeugnisEditor_Erzeugen_NoTemplate;
            return;
        }

        await FlushAsync();

        var schule = await _reporting.Settings.GetAsync();
        var request = new ReportRenderRequest(_zeugnis, _kind, _kind.Klasse ?? new Klasse(), _formular, schule);

        var vorschlag = $"Zeugnis_{Sanitize(_kind.Nachname)}_{Sanitize(_kind.Vorname)}_" +
                        $"{Sanitize(_zeugnis.Schuljahr)}_{(_zeugnis.Halbjahr == Halbjahr.Erstes ? "HJ1" : "HJ2")}.docx";

        var ziel = _reporting.FilePicker.PickSaveFile(
            Strings.ZeugnisEditor_Erzeugen_Dialog_Title, WpfFilePicker.WordFilter, vorschlag);
        if (ziel is null)
        {
            return;
        }

        ErzeugtLaeuft = true;
        try
        {
            var result = await _reporting.Engine.GenerateAsync(request, ziel);
            if (!result.Success)
            {
                ErzeugenStatus = result.ErrorMessage;
                return;
            }

            _zeugnis.GeneratedFilePath = result.FilePath;
            await _service.SaveAsync(_zeugnis);
            OnPropertyChanged(nameof(ZuletztErzeugt));

            ErzeugenStatus = result.Overflows.Count == 0
                ? Strings.ZeugnisEditor_Erzeugen_Ok
                : string.Format(CultureInfo.CurrentCulture, Strings.ZeugnisEditor_Erzeugen_OkOverflow, result.Overflows.Count);

            try
            {
                _reporting.FileLauncher.Open(result.FilePath!);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Erzeugtes Zeugnis konnte nicht automatisch geöffnet werden");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Zeugnis-Erzeugung fehlgeschlagen");
            ErzeugenStatus = ex.Message;
        }
        finally
        {
            ErzeugtLaeuft = false;
        }
    }

    private static string Sanitize(string value)
        => string.Concat((value ?? string.Empty).Where(c => char.IsLetterOrDigit(c) || c is '-' or '_')) is { Length: > 0 } s
            ? s
            : "X";

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

/// <summary>
/// One free-text field: char counter, overflow flag, a Floskel-picker button, and the
/// "häufig genutzt" shortcut list for this field's Bereich (plan §7.2).
/// </summary>
public sealed partial class FreitextEditorViewModel : ObservableObject
{
    private readonly FreitextEintrag _eintrag;
    private readonly FreitextFeldDefinition _feld;
    private readonly IFloskelPickerService _picker;
    private readonly IFloskelnService _floskeln;
    private readonly Kind _kind;
    private readonly int _stufe;
    private readonly Action _onEdited;

    public FreitextEditorViewModel(FreitextEintrag eintrag, FreitextFeldDefinition feld,
        IFloskelPickerService picker, IFloskelnService floskeln, Kind kind, int stufe, Action onEdited)
    {
        _eintrag = eintrag;
        _feld = feld;
        _picker = picker;
        _floskeln = floskeln;
        _kind = kind;
        _stufe = stufe;
        _onEdited = onEdited;
    }

    public string Label => _feld.Name;
    public string? Hinweis => _feld.Hinweistext;
    public int? MaxZeichen => _feld.MaxZeichen;

    /// <summary>Most-used phrases for this field, resolved for the current child.</summary>
    public ObservableCollection<FloskelVorschlagViewModel> Vorschlaege { get; } = [];

    public bool HatVorschlaege => Vorschlaege.Count > 0;

    public async Task LadeVorschlaegeAsync()
    {
        Vorschlaege.Clear();
        try
        {
            foreach (var f in await _floskeln.GetMeistgenutzteAsync(_feld.Name, _stufe, 5))
            {
                var aufgeloest = PlaceholderEngine.Resolve(f.Text, _kind.Vorname, _kind.Geschlecht);
                Vorschlaege.Add(new FloskelVorschlagViewModel(f.Id, aufgeloest, EinfuegenVorschlag));
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Floskel-Vorschläge für {Feld} konnten nicht geladen werden", _feld.Name);
        }

        OnPropertyChanged(nameof(HatVorschlaege));
    }

    private void EinfuegenVorschlag(string aufgeloesterText, Guid floskelId)
    {
        Text = string.IsNullOrWhiteSpace(Text) ? aufgeloesterText : Text.TrimEnd() + " " + aufgeloesterText;
        _ = _floskeln.RegistriereVerwendungAsync(floskelId);
    }

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

/// <summary>One "häufig genutzt" shortcut beside a free-text field: click to append it.</summary>
public sealed partial class FloskelVorschlagViewModel(Guid id, string aufgeloesterText, Action<string, Guid> einfuegen)
    : ObservableObject
{
    public string VollText => aufgeloesterText;

    public string Kurztext => aufgeloesterText.Length <= 90
        ? aufgeloesterText
        : aufgeloesterText[..90].TrimEnd() + "…";

    [RelayCommand]
    private void Einfuegen() => einfuegen(aufgeloesterText, id);
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
