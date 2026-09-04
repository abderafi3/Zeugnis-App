using ZeugnisApp.App.Services;
using ZeugnisApp.App.ViewModels.Import;
using ZeugnisApp.Application.Abstractions;

namespace ZeugnisApp.App.ViewModels.Klassen;

/// <summary>
/// Page-level view model for the Klassen area. Owns the currently shown sub view model and the
/// navigation between the four steps (plan §Sprint 2). No NavigationView drill-down — a single
/// hosted <c>ContentControl</c> keeps the flow testable.
/// </summary>
public sealed class KlassenWorkspaceViewModel : WorkspaceViewModelBase, IKlassenNavigator
{
    private readonly IKlassenService _service;
    private readonly IDialogService _dialogs;
    private readonly ISaveStatusService _saveStatus;
    private readonly Services.ImportServices _import;

    public KlassenWorkspaceViewModel(
        IKlassenService service, IDialogService dialogs, ISaveStatusService saveStatus,
        Services.ImportServices import, IBusyService busy)
        : base(busy)
    {
        _service = service;
        _dialogs = dialogs;
        _saveStatus = saveStatus;
        _import = import;
    }

    public override Task OnNavigatedToAsync()
    {
        if (Current is null)
        {
            ShowKlassenListe();
        }

        return Task.CompletedTask;
    }

    public void ShowKlassenListe()
        => Swap(new KlassenListViewModel(_service, _dialogs, this));

    public void EditKlasse(Guid? klasseId)
        => Swap(new KlasseEditViewModel(_service, _saveStatus, this, klasseId));

    public void ShowKinder(Guid klasseId, string klasseName)
        => Swap(new KinderListViewModel(_service, _dialogs, this, klasseId, klasseName));

    public void EditKind(Guid klasseId, string klasseName, Guid? kindId)
        => Swap(new KindEditViewModel(_service, _saveStatus, this, klasseId, klasseName, kindId));

    public void ImportKinder(Guid klasseId, string klasseName)
        => Swap(new KinderImportWizardViewModel(
            _import.Parser, _import.KinderCommitter, _import.FilePicker, _import.Clipboard, this, klasseId, klasseName));
}

/// <summary>Sub view models that load data when they become the current view.</summary>
public interface IAsyncInitializable
{
    Task InitializeAsync();
}
