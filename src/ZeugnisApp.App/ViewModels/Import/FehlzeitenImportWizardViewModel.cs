using System.Globalization;
using ZeugnisApp.App.Resources;
using ZeugnisApp.App.Services;
using ZeugnisApp.Application.Abstractions;
using ZeugnisApp.Application.Import;
using ZeugnisApp.Domain.Enums;

namespace ZeugnisApp.App.ViewModels.Import;

/// <summary>
/// Import wizard bound to the absence profile (plan §Sprint 7). Reads a class-list export — the
/// school's SchILD "Schüler-Abwesenheit" PDF or an Excel sheet — and matches each row onto a child
/// already in the class, upserting that child's totals for the current term. Runs in a modal window.
/// </summary>
public sealed class FehlzeitenImportWizardViewModel(
    ITabularImport parser,
    IFehlzeitenImportService committer,
    IFilePickerService filePicker,
    IClipboardService clipboard,
    Guid klasseId,
    string klasseName,
    string schuljahr,
    Halbjahr halbjahr,
    Action onFinished)
    : ImportWizardViewModel(parser, filePicker, clipboard)
{
    public override string Title => string.Format(CultureInfo.CurrentCulture, Strings.Fehlzeiten_Import_Title, klasseName);

    public override ImportProfile Profile => ImportProfiles.Fehlzeiten;

    public override bool SupportsPdf => true;

    protected override async Task<ImportSummary> CommitCoreAsync(IReadOnlyList<ImportRow> rows, int uebersprungen)
    {
        var data = rows.Select(FehlzeitenImportMapper.Map).ToList();
        var ergebnis = await committer.CommitAsync(klasseId, schuljahr, halbjahr, data, uebersprungen);

        ImportHinweis = ergebnis.NichtZugeordnet.Count == 0
            ? null
            : string.Format(CultureInfo.CurrentCulture, Strings.Fehlzeiten_Import_Unmatched,
                string.Join(", ", ergebnis.NichtZugeordnet));

        return ergebnis.ToSummary();
    }

    protected override void OnFinished() => onFinished();
}
