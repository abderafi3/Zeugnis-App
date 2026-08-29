using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using Serilog;
using ZeugnisApp.App.Resources;
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

    [ObservableProperty] private string _klassenText = string.Empty;
    [ObservableProperty] private string _kinderText = string.Empty;
    [ObservableProperty] private string _formulareText = string.Empty;
    [ObservableProperty] private string _floskelnText = string.Empty;
    [ObservableProperty] private string _offeneZeugnisseText = string.Empty;

    public override async Task OnNavigatedToAsync()
    {
        IsBusy = true;
        try
        {
            var settings = await settingsService.GetAsync();
            Schulname = string.IsNullOrWhiteSpace(settings.Name)
                ? Strings.Dashboard_SchoolNotConfigured
                : settings.Name;

            var halbjahr = settings.AktuellesHalbjahr == Halbjahr.Erstes
                ? Strings.Halbjahr_First
                : Strings.Halbjahr_Second;
            SchuljahrHalbjahr = string.IsNullOrWhiteSpace(settings.Schuljahr)
                ? halbjahr
                : Fmt(Strings.Dashboard_SchoolYear, settings.Schuljahr, halbjahr);

            var c = await dashboardService.GetCountsAsync();
            KlassenText = Fmt(Strings.Tile_Klassen_Count, c.Klassen);
            KinderText = Fmt(Strings.Tile_Fehlzeiten_Count, c.Kinder);
            FormulareText = Fmt(Strings.Tile_Formulare_Count, c.Formulare);
            FloskelnText = Fmt(Strings.Tile_Floskeln_Count, c.Floskeln);
            OffeneZeugnisseText = Fmt(Strings.Tile_Zeugnisse_Count, c.OffeneZeugnisse);
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

    private static string Fmt(string format, params object[] args)
        => string.Format(CultureInfo.CurrentCulture, format, args);
}
