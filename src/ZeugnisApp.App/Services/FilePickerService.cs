using Microsoft.Win32;

namespace ZeugnisApp.App.Services;

/// <summary>Open-file dialog, abstracted so import view models stay testable.</summary>
public interface IFilePickerService
{
    /// <summary>Returns the chosen path, or null if the teacher cancelled.</summary>
    string? PickFile(string title, string filter);
}

public sealed class FilePickerService : IFilePickerService
{
    public const string ExcelFilter = "Excel-Dateien (*.xlsx;*.xlsm)|*.xlsx;*.xlsm";
    public const string WordFilter = "Word-Dateien (*.docx)|*.docx";
    public const string CsvFilter = "CSV-Dateien (*.csv;*.txt)|*.csv;*.txt";

    public string? PickFile(string title, string filter)
    {
        var dialog = new OpenFileDialog
        {
            Title = title,
            Filter = filter,
            CheckFileExists = true,
            Multiselect = false,
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }
}
