using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ZeugnisApp.App.Resources;
using ZeugnisApp.Application.Abstractions;
using ZeugnisApp.Domain.Entities;
using ZeugnisApp.Domain.Enums;
using ZeugnisApp.Domain.Floskeln;

namespace ZeugnisApp.App.ViewModels.Floskeln;

/// <summary>
/// Floskel-Auswahl dialog (plan §7.2): filtered list + a live preview with the placeholders
/// resolved for a sample child. Standalone in Sprint 4; wired into the Zeugnis-Editor in Sprint 9.
/// </summary>
public sealed partial class FloskelPickerViewModel(IFloskelnService service) : ObservableObject
{
    /// <summary>Raised when the dialog should close. <c>true</c> = a phrase was chosen.</summary>
    public event Action<bool>? RequestClose;

    public ObservableCollection<Floskel> Floskeln { get; } = [];
    public ObservableCollection<string> Bereiche { get; } = [];
    public IReadOnlyList<EnumOption<Geschlecht?>> GeschlechtOptionen => EnumText.GeschlechtOptions;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PreviewText))]
    private Floskel? _selectedFloskel;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PreviewText))]
    private string _sampleName = "Alex";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PreviewText))]
    private Geschlecht? _sampleGeschlecht = Geschlecht.Weiblich;

    [ObservableProperty] private string? _selectedBereich;
    [ObservableProperty] private int _stufeFilter;             // 0 = alle
    [ObservableProperty] private string _searchText = string.Empty;

    public string PreviewText => SelectedFloskel is null
        ? string.Empty
        : PlaceholderEngine.Resolve(SelectedFloskel.Text, SampleName, SampleGeschlecht);

    public string? Result { get; private set; }

    public async Task InitializeAsync()
    {
        Bereiche.Clear();
        Bereiche.Add(Strings.Floskeln_Filter_All);
        foreach (var b in await service.GetBereicheAsync())
        {
            Bereiche.Add(b);
        }

        SelectedBereich = Strings.Floskeln_Filter_All;
        await ReloadAsync();
    }

    partial void OnSelectedBereichChanged(string? value) => _ = ReloadAsync();
    partial void OnStufeFilterChanged(int value) => _ = ReloadAsync();
    partial void OnSearchTextChanged(string value) => _ = ReloadAsync();

    private async Task ReloadAsync()
    {
        var filter = new FloskelFilter(
            Bereich: string.IsNullOrEmpty(SelectedBereich) || SelectedBereich == Strings.Floskeln_Filter_All ? null : SelectedBereich,
            Stufe: StufeFilter == 0 ? null : StufeFilter,
            SearchText: string.IsNullOrWhiteSpace(SearchText) ? null : SearchText);

        Floskeln.Clear();
        foreach (var f in await service.GetAsync(filter))
        {
            Floskeln.Add(f);
        }

        SelectedFloskel = Floskeln.FirstOrDefault();
    }

    [RelayCommand]
    private void Einfuegen()
    {
        if (SelectedFloskel is null)
        {
            return;
        }

        Result = PreviewText;
        RequestClose?.Invoke(true);
    }

    [RelayCommand]
    private void Abbrechen() => RequestClose?.Invoke(false);
}
