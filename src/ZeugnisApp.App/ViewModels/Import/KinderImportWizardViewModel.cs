using System.Globalization;
using ZeugnisApp.App.Resources;
using ZeugnisApp.App.Services;
using ZeugnisApp.App.ViewModels.Klassen;
using ZeugnisApp.Application.Abstractions;
using ZeugnisApp.Application.Import;

namespace ZeugnisApp.App.ViewModels.Import;

/// <summary>Import wizard bound to the class-list profile, committing into one class (plan §Sprint 3).</summary>
public sealed class KinderImportWizardViewModel(
    ITabularImport parser,
    IKinderImportService committer,
    IFilePickerService filePicker,
    IClipboardService clipboard,
    IKlassenNavigator navigator,
    Guid klasseId,
    string klasseName)
    : ImportWizardViewModel(parser, filePicker, clipboard)
{
    public override string Title => string.Format(CultureInfo.CurrentCulture, Strings.Import_Title, klasseName);

    public override ImportProfile Profile => ImportProfiles.Kinder;

    protected override Task<ImportSummary> CommitCoreAsync(IReadOnlyList<ImportRow> rows, int uebersprungen)
    {
        var data = rows.Select(KinderImportMapper.Map).ToList();
        return committer.CommitAsync(klasseId, data, uebersprungen);
    }

    protected override void OnFinished() => navigator.ShowKinder(klasseId, klasseName);
}
