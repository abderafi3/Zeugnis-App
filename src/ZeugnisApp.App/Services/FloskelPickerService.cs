using Microsoft.Extensions.DependencyInjection;
using ZeugnisApp.App.Resources;
using ZeugnisApp.App.ViewModels.Floskeln;
using ZeugnisApp.App.Views.Floskeln;
using ZeugnisApp.Domain.Enums;

namespace ZeugnisApp.App.Services;

/// <summary>Opens the Floskel-Auswahl dialog and returns the resolved text, or null if cancelled.</summary>
public interface IFloskelPickerService
{
    Task<string?> PickAsync(string sampleName = "Alex", Geschlecht? sampleGeschlecht = Geschlecht.Weiblich);
}

public sealed class FloskelPickerService(IServiceProvider services, IBusyService busy) : IFloskelPickerService
{
    public async Task<string?> PickAsync(string sampleName = "Alex", Geschlecht? sampleGeschlecht = Geschlecht.Weiblich)
    {
        var vm = services.GetRequiredService<FloskelPickerViewModel>();
        vm.SampleName = sampleName;
        vm.SampleGeschlecht = sampleGeschlecht;

        // The seeded library is ~2300 phrases, so the load before the dialog appears is felt.
        await busy.RunAsync(vm.InitializeAsync, Strings.Busy_Floskeln);

        var window = new FloskelPickerWindow(vm)
        {
            Owner = System.Windows.Application.Current.MainWindow,
        };

        return window.ShowDialog() == true ? vm.Result : null;
    }
}
