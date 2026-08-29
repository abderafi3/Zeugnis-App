using CommunityToolkit.Mvvm.ComponentModel;
using ZeugnisApp.App.Resources;
using ZeugnisApp.Application.Import;

namespace ZeugnisApp.App.ViewModels.Klassen;

/// <summary>One column on the wizard's "Spalten prüfen" step: header, sample values, chosen role.</summary>
public sealed partial class ColumnMappingViewModel : ObservableObject
{
    private readonly Action<int, string> _onRoleChanged;

    public ColumnMappingViewModel(int index, string header, string sampleText, string roleKey,
        IReadOnlyList<EnumOption<string>> roleOptions, Action<int, string> onRoleChanged)
    {
        Index = index;
        Header = header;
        SampleText = sampleText;
        RoleOptions = roleOptions;
        _selectedRoleKey = roleKey;
        _onRoleChanged = onRoleChanged;
    }

    public int Index { get; }
    public string Header { get; }
    public string SampleText { get; }
    public IReadOnlyList<EnumOption<string>> RoleOptions { get; }

    [ObservableProperty]
    private string _selectedRoleKey;

    partial void OnSelectedRoleKeyChanged(string value) => _onRoleChanged(Index, value);
}

/// <summary>One editable row on the wizard's preview/correction step (plan §6, step 3).</summary>
public sealed partial class ImportRowViewModel(ImportRow row, ImportProfile profile) : ObservableObject
{
    public int SourceRowNumber => row.SourceRowNumber;

    public string Vorname
    {
        get => row.Value(ImportFieldKeys.Vorname);
        set => SetCell(ImportFieldKeys.Vorname, value);
    }

    public string Nachname
    {
        get => row.Value(ImportFieldKeys.Nachname);
        set => SetCell(ImportFieldKeys.Nachname, value);
    }

    public string Geburtsdatum
    {
        get => row.Value(ImportFieldKeys.Geburtsdatum);
        set => SetCell(ImportFieldKeys.Geburtsdatum, value);
    }

    public string Geschlecht
    {
        get => row.Value(ImportFieldKeys.Geschlecht);
        set => SetCell(ImportFieldKeys.Geschlecht, value);
    }

    public bool Included
    {
        get => row.Included;
        set
        {
            if (row.Included == value)
            {
                return;
            }

            row.Included = value;
            OnPropertyChanged();
        }
    }

    public bool IsValid => row.IsValid;

    public string ErrorText => string.Join(" · ", row.Errors);

    public ImportRow Model => row;

    private void SetCell(string key, string value)
    {
        if (row.Value(key) == value)
        {
            return;
        }

        row.Values[key] = value ?? string.Empty;
        row.Revalidate(profile);
        OnPropertyChanged(nameof(Vorname));
        OnPropertyChanged(nameof(Nachname));
        OnPropertyChanged(nameof(Geburtsdatum));
        OnPropertyChanged(nameof(Geschlecht));
        OnPropertyChanged(nameof(IsValid));
        OnPropertyChanged(nameof(ErrorText));
        OnPropertyChanged(nameof(Included));
    }
}
