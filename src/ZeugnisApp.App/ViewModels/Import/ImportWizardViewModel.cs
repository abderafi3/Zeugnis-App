using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using ZeugnisApp.App.Resources;
using ZeugnisApp.App.Services;
using ZeugnisApp.Application.Abstractions;
using ZeugnisApp.Application.Import;

namespace ZeugnisApp.App.ViewModels.Import;

/// <summary>
/// The reusable 3-step import wizard (plan §6 / §7.2): Quelle wählen → Spalten prüfen →
/// Vorschau/Korrektur. Subclasses supply the <see cref="Profile"/>, the commit and the exit.
/// </summary>
public abstract partial class ImportWizardViewModel : ObservableObject
{
    private readonly ITabularImport _parser;
    private readonly IFilePickerService _filePicker;
    private readonly IClipboardService _clipboard;

    protected ImportWizardViewModel(ITabularImport parser, IFilePickerService filePicker, IClipboardService clipboard)
    {
        _parser = parser;
        _filePicker = filePicker;
        _clipboard = clipboard;
    }

    protected ImportSession? Session { get; private set; }

    public abstract string Title { get; }

    public abstract ImportProfile Profile { get; }

    protected abstract Task<ImportSummary> CommitCoreAsync(IReadOnlyList<ImportRow> rows, int uebersprungen);

    protected abstract void OnFinished();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsStep1), nameof(IsStep2), nameof(IsStep3))]
    private int _step = 1;

    public bool IsStep1 => Step == 1;
    public bool IsStep2 => Step == 2;
    public bool IsStep3 => Step == 3;

    [ObservableProperty] private string? _sourceError;

    // ---- Step 2 ----
    public ObservableCollection<ColumnMappingViewModel> ColumnMappings { get; } = [];

    [ObservableProperty] private bool _hasHeaderRow;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanContinueToPreview))]
    private string _mappingHint = string.Empty;

    public bool CanContinueToPreview => Session?.AllRequiredMapped == true;

    // ---- Step 3 ----
    public ObservableCollection<ImportRowViewModel> PreviewRows { get; } = [];

    [ObservableProperty] private string _previewSummary = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasImported))]
    private string? _resultSummary;

    public bool HasImported => ResultSummary is not null;

    // ================= Step 1 =================

    [RelayCommand]
    private void LoadExcel() => LoadFromFile(Strings.Import_FromExcel, FilePickerService.ExcelFilter,
        p => { using var s = File.OpenRead(p); return _parser.FromExcel(s); });

    [RelayCommand]
    private void LoadWord() => LoadFromFile(Strings.Import_FromWord, FilePickerService.WordFilter,
        p => { using var s = File.OpenRead(p); return _parser.FromWordTable(s); });

    [RelayCommand]
    private void LoadCsv() => LoadFromFile(Strings.Import_FromCsv, FilePickerService.CsvFilter,
        p => { using var s = File.OpenRead(p); return _parser.FromCsv(s); });

    [RelayCommand]
    private void PasteFromClipboard()
    {
        SourceError = null;
        try
        {
            var text = _clipboard.GetText();
            if (string.IsNullOrWhiteSpace(text))
            {
                SourceError = Strings.Import_Error_Empty;
                return;
            }

            StartSession(_parser.FromDelimitedText(text));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Zwischenablage-Import fehlgeschlagen");
            SourceError = string.Format(CultureInfo.CurrentCulture, Strings.Import_Error_Read, ex.Message);
        }
    }

    /// <summary>Entry point for the view's drag-and-drop zone.</summary>
    public void LoadDroppedFile(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        Func<string, TabularGrid>? read = ext switch
        {
            ".xlsx" or ".xlsm" => p => { using var s = File.OpenRead(p); return _parser.FromExcel(s); },
            ".docx" => p => { using var s = File.OpenRead(p); return _parser.FromWordTable(s); },
            ".csv" or ".txt" => p => { using var s = File.OpenRead(p); return _parser.FromCsv(s); },
            _ => null,
        };

        if (read is null)
        {
            SourceError = Strings.Import_Error_Empty;
            return;
        }

        RunRead(read, path);
    }

    private void LoadFromFile(string title, string filter, Func<string, TabularGrid> read)
    {
        SourceError = null;
        var path = _filePicker.PickFile(title, filter);
        if (path is not null)
        {
            RunRead(read, path);
        }
    }

    private void RunRead(Func<string, TabularGrid> read, string path)
    {
        SourceError = null;
        try
        {
            StartSession(read(path));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Import aus {Path} fehlgeschlagen", path);
            SourceError = string.Format(CultureInfo.CurrentCulture, Strings.Import_Error_Read, ex.Message);
        }
    }

    private void StartSession(TabularGrid grid)
    {
        if (grid.RowCount == 0 || grid.ColumnCount == 0)
        {
            SourceError = Strings.Import_Error_Empty;
            return;
        }

        Session = new ImportSession(grid, Profile);
        HasHeaderRow = Session.HasHeaderRow;
        RebuildColumnMappings();
        Step = 2;
    }

    // ================= Step 2 =================

    partial void OnHasHeaderRowChanged(bool value)
    {
        if (Session is null || Session.HasHeaderRow == value)
        {
            return;
        }

        Session.HasHeaderRow = value;
        RebuildColumnMappings();
    }

    private void RebuildColumnMappings()
    {
        if (Session is null)
        {
            return;
        }

        var options = new List<EnumOption<string>> { new(ImportFieldKeys.Ignore, Strings.Import_Role_Ignore) };
        options.AddRange(Session.Profile.Fields.Select(f => new EnumOption<string>(f.Key, f.DisplayName)));

        ColumnMappings.Clear();
        for (var col = 0; col < Session.Grid.ColumnCount; col++)
        {
            var role = Session.ColumnRoles.GetValueOrDefault(col, ImportFieldKeys.Ignore);
            var sample = string.Join(" · ", Session.SampleValues(col));
            ColumnMappings.Add(new ColumnMappingViewModel(col, Session.ColumnHeader(col), sample, role, options, OnColumnRoleChanged));
        }

        UpdateMappingHint();
    }

    private void OnColumnRoleChanged(int column, string roleKey)
    {
        if (Session is null)
        {
            return;
        }

        if (roleKey != ImportFieldKeys.Ignore)
        {
            foreach (var other in ColumnMappings.Where(m => m.Index != column && m.SelectedRoleKey == roleKey))
            {
                other.SelectedRoleKey = ImportFieldKeys.Ignore;
            }
        }

        Session.ColumnRoles[column] = roleKey;
        UpdateMappingHint();
    }

    private void UpdateMappingHint()
    {
        if (Session is null)
        {
            return;
        }

        MappingHint = Session.AllRequiredMapped
            ? Strings.Import_MappingOk
            : string.Format(CultureInfo.CurrentCulture, Strings.Import_MappingMissing,
                string.Join(", ", Session.UnmappedRequiredFields));

        OnPropertyChanged(nameof(CanContinueToPreview));
        ContinueToPreviewCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private void BackToSource()
    {
        Step = 1;
        Session = null;
        ColumnMappings.Clear();
    }

    [RelayCommand(CanExecute = nameof(CanContinueToPreview))]
    private void ContinueToPreview()
    {
        if (Session is null)
        {
            return;
        }

        PreviewRows.Clear();
        foreach (var row in Session.BuildRows())
        {
            PreviewRows.Add(new ImportRowViewModel(row, Session.Profile));
        }

        UpdatePreviewSummary();
        ResultSummary = null;
        Step = 3;
    }

    // ================= Step 3 =================

    private void UpdatePreviewSummary()
    {
        var valid = PreviewRows.Count(r => r.IsValid);
        PreviewSummary = string.Format(CultureInfo.CurrentCulture, Strings.Import_PreviewSummary,
            valid, PreviewRows.Count - valid);
    }

    [RelayCommand]
    private void BackToMapping() => Step = 2;

    [RelayCommand]
    private async Task DoImportAsync()
    {
        UpdatePreviewSummary();

        var chosen = PreviewRows.Where(r => r.Included && r.IsValid).Select(r => r.Model).ToList();
        var skipped = PreviewRows.Count - chosen.Count;

        try
        {
            var summary = await CommitCoreAsync(chosen, skipped);
            ResultSummary = string.Format(CultureInfo.CurrentCulture, Strings.Import_ResultSummary,
                summary.Importiert, summary.Uebersprungen);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Import-Commit fehlgeschlagen");
            SourceError = string.Format(CultureInfo.CurrentCulture, Strings.Import_Error_Read, ex.Message);
        }
    }

    [RelayCommand]
    private void Finish() => OnFinished();
}
