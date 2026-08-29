using ZeugnisApp.Application.Abstractions;

namespace ZeugnisApp.App.Services;

/// <summary>Bundles the services the import wizard needs, to keep view-model constructors small.</summary>
public sealed class ImportServices(
    ITabularImport parser,
    IKinderImportService committer,
    IFilePickerService filePicker,
    IClipboardService clipboard)
{
    public ITabularImport Parser { get; } = parser;
    public IKinderImportService Committer { get; } = committer;
    public IFilePickerService FilePicker { get; } = filePicker;
    public IClipboardService Clipboard { get; } = clipboard;
}
