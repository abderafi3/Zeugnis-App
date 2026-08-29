using CommunityToolkit.Mvvm.ComponentModel;

namespace ZeugnisApp.App.ViewModels;

/// <summary>Common base for all view models. Screens are testable via VM + service, no UI (plan §3).</summary>
public abstract partial class ViewModelBase : ObservableObject
{
    [ObservableProperty]
    private bool _isBusy;

    /// <summary>Called by the view when it is navigated to. Override to load data.</summary>
    public virtual Task OnNavigatedToAsync() => Task.CompletedTask;
}
