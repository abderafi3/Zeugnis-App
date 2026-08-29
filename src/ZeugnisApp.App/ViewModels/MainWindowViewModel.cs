using ZeugnisApp.App.Services;

namespace ZeugnisApp.App.ViewModels;

public sealed class MainWindowViewModel(ISaveStatusService saveStatus)
{
    public const string ApplicationTitle = "Zeugnis-App";

    /// <summary>Bound by the shell status bar to show "Gespeichert um …".</summary>
    public ISaveStatusService SaveStatus { get; } = saveStatus;
}
