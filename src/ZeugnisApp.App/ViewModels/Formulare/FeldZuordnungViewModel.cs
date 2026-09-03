using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ZeugnisApp.App.Resources;
using ZeugnisApp.Application.Abstractions;
using ZeugnisApp.Application.Reporting;
using ZeugnisApp.Domain.Entities;

namespace ZeugnisApp.App.ViewModels.Formulare;

/// <summary>
/// The field-mapping wizard (plan §Sprint 10): one row per app value key, each with a dropdown of
/// the content-control tags found in the uploaded <c>.docx</c>. Produces a <see cref="FieldMapping"/>.
/// </summary>
public sealed partial class FeldZuordnungViewModel : ObservableObject
{
    public event Action<bool>? RequestClose;

    private readonly List<ZuordnungZeile> _zeilen = [];

    public FeldZuordnungViewModel(Formular formular, IReadOnlyList<ContentControlInfo> steuerelemente, string? bestehendeZuordnungJson)
    {
        var options = new List<TagOption> { new(null, Strings.Zuordnung_None) };
        options.AddRange(steuerelemente.Select(s => new TagOption(s.Tag, s.Anzeige)));
        TagOptionen = options;

        var bestehend = FieldMapping.FromJson(bestehendeZuordnungJson);
        var standard = FieldMapping.Default(formular);
        var vorhandeneTags = steuerelemente.Select(s => s.Tag).ToHashSet(StringComparer.Ordinal);

        string? Vorauswahl(string valueKey)
        {
            var aus = bestehend.TagFor(valueKey) ?? standard.TagFor(valueKey);
            return aus is not null && vorhandeneTags.Contains(aus) ? aus : null;
        }

        void Add(string gruppe, string valueKey, string label, bool pflicht = false)
            => _zeilen.Add(new ZuordnungZeile(gruppe, valueKey, label, pflicht, Vorauswahl(valueKey), OnZeileGeaendert));

        Add(Strings.Zuordnung_Group_Schule, ValueKeys.SchuleName, Strings.Feld_Schule_Name);
        Add(Strings.Zuordnung_Group_Schule, ValueKeys.SchuleZusatz, Strings.Feld_Schule_Zusatz);
        Add(Strings.Zuordnung_Group_Schule, ValueKeys.SchuleAdresse, Strings.Feld_Schule_Adresse);
        Add(Strings.Zuordnung_Group_Schule, ValueKeys.SchuleLeitung, Strings.Feld_Schule_Leitung);
        Add(Strings.Zuordnung_Group_Schule, ValueKeys.SchuleSchuljahr, Strings.Feld_Schule_Schuljahr);
        Add(Strings.Zuordnung_Group_Schule, ValueKeys.SchuleHalbjahr, Strings.Feld_Schule_Halbjahr);
        Add(Strings.Zuordnung_Group_Schule, ValueKeys.SchuleKonferenzdatum, Strings.Feld_Schule_Konferenzdatum);
        Add(Strings.Zuordnung_Group_Schule, ValueKeys.SchuleWiederbeginn, Strings.Feld_Schule_Wiederbeginn);

        Add(Strings.Zuordnung_Group_Klasse, ValueKeys.KlasseName, Strings.Feld_Klasse_Name);
        Add(Strings.Zuordnung_Group_Klasse, ValueKeys.KlasseLeitung, Strings.Feld_Klasse_Leitung);
        Add(Strings.Zuordnung_Group_Klasse, ValueKeys.KlasseZeugnisdatum, Strings.Feld_Klasse_Zeugnisdatum);

        Add(Strings.Zuordnung_Group_Kind, ValueKeys.KindName, Strings.Feld_Kind_Name);
        Add(Strings.Zuordnung_Group_Kind, ValueKeys.KindVorname, Strings.Feld_Kind_Vorname);
        Add(Strings.Zuordnung_Group_Kind, ValueKeys.KindNachname, Strings.Feld_Kind_Nachname);
        Add(Strings.Zuordnung_Group_Kind, ValueKeys.KindGeburtsdatum, Strings.Feld_Kind_Geburtsdatum);

        if (formular.HatBegruendeteEmpfehlung)
        {
            Add(Strings.Zuordnung_Group_Zeugnis, ValueKeys.ZeugnisEmpfehlung, Strings.Feld_Zeugnis_Empfehlung, pflicht: true);
            Add(Strings.Zuordnung_Group_Zeugnis, ValueKeys.ZeugnisEmpfehlungBegruendung, Strings.Feld_Zeugnis_Begruendung, pflicht: true);
        }

        if (formular.HatVersetzungsvermerk)
        {
            // Optional: a template without a dedicated control still gets the Versetzungsvermerk —
            // ReportValues folds it into the Bemerkungen text.
            Add(Strings.Zuordnung_Group_Zeugnis, ValueKeys.ZeugnisVersetzungsvermerk, Strings.Feld_Zeugnis_Versetzung);
        }

        Add(Strings.Zuordnung_Group_Zeugnis, ValueKeys.ZeugnisBemerkungen, Strings.Feld_Zeugnis_Bemerkungen);
        Add(Strings.Zuordnung_Group_Zeugnis, ValueKeys.ZeugnisFehlstundenGesamt, Strings.Feld_Zeugnis_FehlGesamt);
        Add(Strings.Zuordnung_Group_Zeugnis, ValueKeys.ZeugnisFehlstundenUnentschuldigt, Strings.Feld_Zeugnis_FehlUnentschuldigt);

        if (formular.HatNotenblock)
        {
            foreach (var fach in formular.Faecher)
            {
                Add(Strings.Zuordnung_Group_Noten, ValueKeys.Note(fach), fach, pflicht: true);
            }
        }

        foreach (var feld in formular.Freitextfelder)
        {
            Add(Strings.Zuordnung_Group_Freitexte, ValueKeys.Freitext(feld.Name), feld.Name, pflicht: true);
        }

        Zeilen = new ObservableCollection<ZuordnungZeile>(_zeilen);
        UpdateHinweis();
    }

    public IReadOnlyList<TagOption> TagOptionen { get; }
    public ObservableCollection<ZuordnungZeile> Zeilen { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AllePflichtZugeordnet))]
    private string _hinweis = string.Empty;

    public bool AllePflichtZugeordnet => !PflichtOffen().Any();

    public FieldMapping? Ergebnis { get; private set; }

    [RelayCommand(CanExecute = nameof(AllePflichtZugeordnet))]
    private void Uebernehmen()
    {
        var mapping = new FieldMapping();
        foreach (var zeile in _zeilen.Where(z => !string.IsNullOrWhiteSpace(z.AusgewaehltesTag)))
        {
            mapping.Tags[zeile.ValueKey] = zeile.AusgewaehltesTag!;
        }

        Ergebnis = mapping;
        RequestClose?.Invoke(true);
    }

    [RelayCommand]
    private void Abbrechen() => RequestClose?.Invoke(false);

    private void OnZeileGeaendert() => UpdateHinweis();

    private void UpdateHinweis()
    {
        var offen = PflichtOffen().ToList();
        Hinweis = offen.Count == 0
            ? string.Empty
            : string.Format(CultureInfo.CurrentCulture, Strings.Zuordnung_Missing, string.Join(", ", offen));

        OnPropertyChanged(nameof(AllePflichtZugeordnet));
        UebernehmenCommand.NotifyCanExecuteChanged();
    }

    private IEnumerable<string> PflichtOffen()
        => _zeilen.Where(z => z.IstPflicht && string.IsNullOrWhiteSpace(z.AusgewaehltesTag)).Select(z => z.Label);
}

public sealed partial class ZuordnungZeile(
    string gruppe, string valueKey, string label, bool istPflicht, string? ausgewaehltesTag, Action onChanged)
    : ObservableObject
{
    public string Gruppe { get; } = gruppe;
    public string ValueKey { get; } = valueKey;
    public string Label { get; } = label;
    public bool IstPflicht { get; } = istPflicht;

    [ObservableProperty]
    private string? _ausgewaehltesTag = ausgewaehltesTag;

    partial void OnAusgewaehltesTagChanged(string? value) => onChanged();
}

public sealed record TagOption(string? Tag, string Anzeige);
