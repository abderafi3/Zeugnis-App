using System.Globalization;
using Serilog;
using ZeugnisApp.App.Resources;
using ZeugnisApp.App.ViewModels.Formulare;
using ZeugnisApp.App.Views.Formulare;
using ZeugnisApp.Application.Abstractions;

namespace ZeugnisApp.App.Services;

/// <summary>Runs the Sprint 10 flow: pick a <c>.docx</c> → inspect → map its content controls → link it.</summary>
public interface ITemplateUploadService
{
    /// <summary>Returns true if a template was uploaded and mapped.</summary>
    Task<bool> UploadAndMapAsync(Guid formularId);
}

public sealed class TemplateUploadService(
    IFilePickerService filePicker,
    ITemplateInspector inspector,
    IFormulareService formulareService,
    IDialogService dialogs)
    : ITemplateUploadService
{
    public async Task<bool> UploadAndMapAsync(Guid formularId)
    {
        var formular = await formulareService.GetAsync(formularId);
        if (formular is null)
        {
            return false;
        }

        var path = filePicker.PickFile(Strings.FormularEdit_WordFile_Upload, FilePickerService.WordFilter);
        if (path is null)
        {
            return false;
        }

        IReadOnlyList<ContentControlInfo> controls;
        try
        {
            controls = inspector.ReadContentControls(path);
        }
        catch (InvalidTemplateException ex)
        {
            await dialogs.ConfirmAsync(Strings.Zuordnung_Title,
                string.Format(CultureInfo.CurrentCulture, Strings.Zuordnung_Error_Invalid, ex.Message),
                okText: Strings.Common_Yes, cancelText: Strings.Common_No);
            return false;
        }

        if (controls.Count == 0)
        {
            await dialogs.ConfirmAsync(Strings.Zuordnung_Title, Strings.Zuordnung_Hint_NoControls,
                okText: Strings.Common_Yes, cancelText: Strings.Common_No);
            return false;
        }

        var vm = new FeldZuordnungViewModel(formular, controls, formular.FieldMappingJson);
        var window = new FeldZuordnungWindow(vm)
        {
            Owner = System.Windows.Application.Current.MainWindow,
        };

        if (window.ShowDialog() != true || vm.Ergebnis is null)
        {
            return false;
        }

        try
        {
            await formulareService.LinkTemplateAsync(formularId, path, vm.Ergebnis.ToJson());
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Vorlage konnte nicht verknüpft werden");
            return false;
        }
    }
}
