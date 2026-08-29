using CommunityToolkit.Mvvm.ComponentModel;
using ZeugnisApp.App.Resources;
using ZeugnisApp.Application.Import;

namespace ZeugnisApp.App.ViewModels.Import;

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

/// <summary>One editable cell on the wizard's preview step. Writing re-validates the whole row.</summary>
public sealed partial class ImportCellViewModel(ImportRow row, string fieldKey, string header, Action revalidate)
    : ObservableObject
{
    public string FieldKey { get; } = fieldKey;
    public string Header { get; } = header;

    public string Value
    {
        get => row.Value(FieldKey);
        set
        {
            if (row.Value(FieldKey) == value)
            {
                return;
            }

            row.Values[FieldKey] = value ?? string.Empty;
            revalidate();
        }
    }

    internal void RaiseChanged() => OnPropertyChanged(nameof(Value));
}

/// <summary>
/// One editable row on the wizard's preview/correction step (plan §6, step 3). Profile-generic:
/// <see cref="Cells"/> is built from the active <see cref="ImportProfile"/>.
/// </summary>
public sealed partial class ImportRowViewModel : ObservableObject
{
    private readonly ImportRow _row;
    private readonly ImportProfile _profile;

    public ImportRowViewModel(ImportRow row, ImportProfile profile)
    {
        _row = row;
        _profile = profile;
        Cells = profile.Fields
            .Select(f => new ImportCellViewModel(row, f.Key, f.DisplayName, Revalidate))
            .ToList();
    }

    public IReadOnlyList<ImportCellViewModel> Cells { get; }

    public int SourceRowNumber => _row.SourceRowNumber;

    public bool Included
    {
        get => _row.Included;
        set
        {
            if (_row.Included == value)
            {
                return;
            }

            _row.Included = value;
            OnPropertyChanged();
        }
    }

    public bool IsValid => _row.IsValid;

    public string ErrorText => string.Join(" · ", _row.Errors);

    public ImportRow Model => _row;

    public string Cell(string fieldKey) => _row.Value(fieldKey);

    private void Revalidate()
    {
        _row.Revalidate(_profile);
        foreach (var cell in Cells)
        {
            cell.RaiseChanged();
        }

        OnPropertyChanged(nameof(IsValid));
        OnPropertyChanged(nameof(ErrorText));
        OnPropertyChanged(nameof(Included));
    }
}
