using ZeugnisApp.App.Resources;
using ZeugnisApp.App.Services;
using ZeugnisApp.Application.Abstractions;
using ZeugnisApp.Application.Import;

namespace ZeugnisApp.App.ViewModels.Import;

/// <summary>
/// Import wizard bound to the phrase-library profile (plan §Sprint 4 — reuses the Sprint 3 pipeline).
/// Runs in a modal window; <paramref name="onFinished"/> closes it.
/// </summary>
public sealed class FloskelnImportWizardViewModel(
    ITabularImport parser,
    IFloskelImportService committer,
    IFilePickerService filePicker,
    IClipboardService clipboard,
    Action onFinished)
    : ImportWizardViewModel(parser, filePicker, clipboard)
{
    public override string Title => Strings.Floskeln_Import;

    public override ImportProfile Profile => ImportProfiles.Floskeln;

    protected override Task<ImportSummary> CommitCoreAsync(IReadOnlyList<ImportRow> rows, int uebersprungen)
    {
        var data = rows.Select(FloskelImportMapper.Map).ToList();
        return committer.CommitAsync(data, uebersprungen);
    }

    protected override void OnFinished() => onFinished();
}
