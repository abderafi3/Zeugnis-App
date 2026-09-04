using ZeugnisApp.App.Services;

namespace ZeugnisApp.App.ViewModels;

public sealed class MainWindowViewModel(ISaveStatusService saveStatus, IBusyService busy)
{
    public const string ApplicationTitle = "Zeugnis-App";

    /// <summary>Bound by the shell status bar to show "Gespeichert um …".</summary>
    public ISaveStatusService SaveStatus { get; } = saveStatus;

    /// <summary>Bound by the shell overlay to show a spinner while a screen is still loading.</summary>
    public IBusyService Busy { get; } = busy;
}
