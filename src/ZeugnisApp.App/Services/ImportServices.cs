using ZeugnisApp.Application.Abstractions;

namespace ZeugnisApp.App.Services;

/// <summary>Bundles the services the import wizards need, to keep view-model constructors small.</summary>
public sealed class ImportServices(
    ITabularImport parser,
    IKinderImportService kinderCommitter,
    IFloskelImportService floskelCommitter,
    IFehlzeitenImportService fehlzeitenCommitter,
    IFilePickerService filePicker,
    IClipboardService clipboard)
{
    public ITabularImport Parser { get; } = parser;
    public IKinderImportService KinderCommitter { get; } = kinderCommitter;
    public IFloskelImportService FloskelCommitter { get; } = floskelCommitter;
    public IFehlzeitenImportService FehlzeitenCommitter { get; } = fehlzeitenCommitter;
    public IFilePickerService FilePicker { get; } = filePicker;
    public IClipboardService Clipboard { get; } = clipboard;
}
