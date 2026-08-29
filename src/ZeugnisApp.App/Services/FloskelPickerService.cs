using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using ZeugnisApp.App.ViewModels.Floskeln;
using ZeugnisApp.App.Views.Floskeln;
using ZeugnisApp.Domain.Enums;

namespace ZeugnisApp.App.Services;

/// <summary>Opens the Floskel-Auswahl dialog and returns the resolved text, or null if cancelled.</summary>
public interface IFloskelPickerService
{
    Task<string?> PickAsync(string sampleName = "Alex", Geschlecht? sampleGeschlecht = Geschlecht.Weiblich);
}

public sealed class FloskelPickerService(IServiceProvider services) : IFloskelPickerService
{
    public async Task<string?> PickAsync(string sampleName = "Alex", Geschlecht? sampleGeschlecht = Geschlecht.Weiblich)
    {
        var vm = services.GetRequiredService<FloskelPickerViewModel>();
        vm.SampleName = sampleName;
        vm.SampleGeschlecht = sampleGeschlecht;
        await vm.InitializeAsync();

        var window = new FloskelPickerWindow(vm)
        {
            Owner = System.Windows.Application.Current.MainWindow,
        };

        return window.ShowDialog() == true ? vm.Result : null;
    }
}
