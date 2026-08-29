using ZeugnisApp.Application.Abstractions;

namespace ZeugnisApp.App.Services;

/// <summary>Bundles what the Zeugnis editor needs to generate a <c>.docx</c>, to keep its constructor small.</summary>
public sealed class ReportGenerationServices(
    IReportGenerationEngine engine,
    IFilePickerService filePicker,
    IFileLauncher fileLauncher,
    ISchoolSettingsService settings)
{
    public IReportGenerationEngine Engine { get; } = engine;
    public IFilePickerService FilePicker { get; } = filePicker;
    public IFileLauncher FileLauncher { get; } = fileLauncher;
    public ISchoolSettingsService Settings { get; } = settings;
}
