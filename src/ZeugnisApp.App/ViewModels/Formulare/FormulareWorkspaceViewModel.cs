using ZeugnisApp.App.Services;
using ZeugnisApp.Application.Abstractions;

namespace ZeugnisApp.App.ViewModels.Formulare;

public interface IFormulareNavigator
{
    void ShowListe();
    void Edit(Guid? formularId);
}

/// <summary>Page-level view model for the Formulare area: list ↔ edit (plan §Sprint 5 / §Sprint 10).</summary>
public sealed class FormulareWorkspaceViewModel(
    IFormulareService service, IDialogService dialogs, ISaveStatusService saveStatus,
    ITemplateUploadService templateUpload, IBusyService busy)
    : WorkspaceViewModelBase(busy), IFormulareNavigator
{
    public override Task OnNavigatedToAsync()
    {
        if (Current is null)
        {
            ShowListe();
        }

        return Task.CompletedTask;
    }

    public void ShowListe() => Swap(new FormulareListViewModel(service, dialogs, this));

    public void Edit(Guid? formularId) => Swap(new FormularEditViewModel(service, saveStatus, this, formularId, templateUpload));
}
