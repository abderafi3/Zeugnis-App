using CommunityToolkit.Mvvm.ComponentModel;
using Serilog;
using ZeugnisApp.Application.Abstractions;
using ZeugnisApp.Domain.Enums;

namespace ZeugnisApp.App.ViewModels;

/// <summary>
/// Start screen: five large tiles with live counts (plan §7.2).
/// </summary>
public sealed partial class DashboardViewModel(IDashboardService dashboardService, ISchoolSettingsService settingsService)
    : ViewModelBase
{
    [ObservableProperty] private string _schulname = string.Empty;
    [ObservableProperty] private string _schuljahrHalbjahr = string.Empty;

    [ObservableProperty] private int _klassenAnzahl;
    [ObservableProperty] private int _kinderAnzahl;
    [ObservableProperty] private int _formulareAnzahl;
    [ObservableProperty] private int _floskelnAnzahl;
    [ObservableProperty] private int _offeneZeugnisseAnzahl;

    public override async Task OnNavigatedToAsync()
    {
        IsBusy = true;
        try
        {
            var settings = await settingsService.GetAsync();
            Schulname = string.IsNullOrWhiteSpace(settings.Name) ? "Schule noch nicht eingerichtet" : settings.Name;
            var halbjahr = settings.AktuellesHalbjahr == Halbjahr.Erstes ? "1. Halbjahr" : "2. Halbjahr";
            SchuljahrHalbjahr = string.IsNullOrWhiteSpace(settings.Schuljahr)
                ? halbjahr
                : $"Schuljahr {settings.Schuljahr} · {halbjahr}";

            var counts = await dashboardService.GetCountsAsync();
            KlassenAnzahl = counts.Klassen;
            KinderAnzahl = counts.Kinder;
            FormulareAnzahl = counts.Formulare;
            FloskelnAnzahl = counts.Floskeln;
            OffeneZeugnisseAnzahl = counts.OffeneZeugnisse;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Dashboard konnte nicht geladen werden");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
