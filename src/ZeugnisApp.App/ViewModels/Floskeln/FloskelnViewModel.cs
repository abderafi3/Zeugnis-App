using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using ZeugnisApp.App.Resources;
using ZeugnisApp.App.Services;
using ZeugnisApp.App.ViewModels.Import;
using ZeugnisApp.App.Views.Import;
using ZeugnisApp.Application.Abstractions;
using ZeugnisApp.Domain.Entities;
using ZeugnisApp.Domain.Enums;

namespace ZeugnisApp.App.ViewModels.Floskeln;

/// <summary>
/// The phrase-library screen (plan §7.2): left filter panel + searchable list with inline edit,
/// Excel import (reusing the Sprint 3 wizard), and a "test the picker" action.
/// </summary>
public sealed partial class FloskelnViewModel(
    IFloskelnService service,
    IDialogService dialogs,
    ImportServices import,
    IFloskelPickerService picker,
    ISaveStatusService saveStatus)
    : ViewModelBase
{
    public ObservableCollection<FloskelRowViewModel> Floskeln { get; } = [];
    public ObservableCollection<string> Bereiche { get; } = [];
    public IReadOnlyList<EnumOption<Klassengruppe>> KlassengruppeOptionen => EnumText.KlassengruppeOptions;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    private bool _loaded;

    [ObservableProperty] private string? _selectedBereich;
    [ObservableProperty] private int _stufeFilter;                 // 0 = alle
    [ObservableProperty] private string _searchText = string.Empty;

    [ObservableProperty] private string? _pickerResult;

    public bool IsEmpty => Loaded && Floskeln.Count == 0;

    private int _reloadGeneration;

    public override async Task OnNavigatedToAsync()
    {
        var bereiche = await service.GetBereicheAsync();

        Bereiche.Clear();
        Bereiche.Add(Strings.Floskeln_Filter_All);
        foreach (var b in bereiche)
        {
            Bereiche.Add(b);
        }

        SelectedBereich ??= Strings.Floskeln_Filter_All;
        await ReloadAsync();
    }

    partial void OnSelectedBereichChanged(string? value) => _ = ReloadAsync();
    partial void OnStufeFilterChanged(int value) => _ = ReloadAsync();
    partial void OnSearchTextChanged(string value) => _ = ReloadAsync();

    /// <summary>The currently filtered category, or null when "all" is selected.</summary>
    private string? ActiveBereich
        => string.IsNullOrEmpty(SelectedBereich) || SelectedBereich == Strings.Floskeln_Filter_All
            ? null
            : SelectedBereich;

    private async Task ReloadAsync()
    {
        // Typing in the search box fires a reload per keystroke; only the last one wins
        // and it replaces the list in a single step so rows never half-populate.
        var generation = ++_reloadGeneration;

        var filter = new FloskelFilter(
            Bereich: ActiveBereich,
            Stufe: StufeFilter == 0 ? null : StufeFilter,
            SearchText: string.IsNullOrWhiteSpace(SearchText) ? null : SearchText);

        List<FloskelRowViewModel> neu;
        try
        {
            var treffer = await service.GetAsync(filter);
            neu = [.. treffer.Select(f => new FloskelRowViewModel(f, service, dialogs, saveStatus, ReloadAsync))];
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Floskeln konnten nicht geladen werden");
            return;
        }

        if (generation != _reloadGeneration)
        {
            return;
        }

        Floskeln.Clear();
        foreach (var row in neu)
        {
            Floskeln.Add(row);
        }

        Loaded = true;
        OnPropertyChanged(nameof(IsEmpty));
    }

    [RelayCommand]
    private async Task NeueFloskelAsync()
    {
        try
        {
            var floskel = new Floskel
            {
                Text = "[Name] …",
                // Follow the active filter: "alle" -> empty Bereich / Stufe 0 = gilt für alle.
                Bereich = ActiveBereich ?? string.Empty,
                Stufe = StufeFilter,
                Klassengruppe = Klassengruppe.Beide,
            };
            await service.SaveAsync(floskel);
            await ReloadAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Neue Floskel konnte nicht angelegt werden");
        }
    }

    [RelayCommand]
    private async Task ImportierenAsync()
    {
        var completion = new TaskCompletionSource();
        var wizard = new FloskelnImportWizardViewModel(
            import.Parser, import.FloskelCommitter, import.FilePicker, import.Clipboard,
            onFinished: () => completion.TrySetResult());

        var window = new ImportWizardWindow(wizard) { Owner = System.Windows.Application.Current.MainWindow };
        window.Closed += (_, _) => completion.TrySetResult();
        window.Show();

        await completion.Task;
        await OnNavigatedToAsync();
    }

    [RelayCommand]
    private async Task AuswahlTestenAsync()
    {
        var text = await picker.PickAsync();
        PickerResult = text is null
            ? null
            : string.Format(CultureInfo.CurrentCulture, Strings.Floskeln_PickerResult, text);
    }
}

/// <summary>One editable row in the library. Inline edits autosave.</summary>
public sealed partial class FloskelRowViewModel(
    Floskel floskel,
    IFloskelnService service,
    IDialogService dialogs,
    ISaveStatusService saveStatus,
    Func<Task> reload)
    : ObservableObject
{
    private readonly AutosaveDebouncer _autosave = Configure(floskel, service, saveStatus);

    public Guid Id => floskel.Id;
    public IReadOnlyList<EnumOption<Klassengruppe>> KlassengruppeOptionen => EnumText.KlassengruppeOptions;

    public string Text
    {
        get => floskel.Text;
        set => SetField(floskel.Text, value, v => floskel.Text = v);
    }

    public string Bereich
    {
        get => floskel.Bereich;
        set => SetField(floskel.Bereich, value, v => floskel.Bereich = v);
    }

    public int Stufe
    {
        // 0 = "gilt für alle Jahrgänge".
        get => floskel.Stufe;
        set => SetField(floskel.Stufe, Math.Clamp(value, 0, 4), v => floskel.Stufe = v);
    }

    public Klassengruppe Klassengruppe
    {
        get => floskel.Klassengruppe;
        set => SetField(floskel.Klassengruppe, value, v => floskel.Klassengruppe = v);
    }

    [RelayCommand]
    private async Task LoeschenAsync()
    {
        var ok = await dialogs.ConfirmAsync(Strings.Floskeln_DeleteConfirm_Title, Strings.Floskeln_DeleteConfirm_Body);
        if (!ok)
        {
            return;
        }

        try
        {
            await service.DeleteAsync(floskel.Id);
            await reload();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Floskel konnte nicht gelöscht werden");
        }
    }

    private void SetField<T>(T current, T value, Action<T> assign,
        [System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(current, value))
        {
            return;
        }

        assign(value);
        OnPropertyChanged(propertyName);
        saveStatus.MarkDirty();
        _autosave.Bump();
    }

    private static AutosaveDebouncer Configure(Floskel floskel, IFloskelnService service, ISaveStatusService saveStatus)
    {
        var debouncer = new AutosaveDebouncer();
        debouncer.Configure(async () =>
        {
            try
            {
                await service.SaveAsync(floskel);
                saveStatus.MarkSaved();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Floskel konnte nicht gespeichert werden");
            }
        });
        return debouncer;
    }
}
