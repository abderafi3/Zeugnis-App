using CommunityToolkit.Mvvm.ComponentModel;

namespace ZeugnisApp.App.ViewModels;

/// <summary>
/// Common base for all view models. Screens are testable via VM + service, no UI (plan §3).
/// Loading state is not held here: it belongs to the shell, which shows one overlay for the
/// whole wait (see <see cref="Services.IBusyService"/>).
/// </summary>
public abstract class ViewModelBase : ObservableObject
{
    /// <summary>Called by the view when it is navigated to. Override to load data.</summary>
    public virtual Task OnNavigatedToAsync() => Task.CompletedTask;
}
