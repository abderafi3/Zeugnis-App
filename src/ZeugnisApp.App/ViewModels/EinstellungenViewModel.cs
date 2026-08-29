using System.ComponentModel;
using System.Globalization;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using ZeugnisApp.App.Resources;
using ZeugnisApp.App.Services;
using ZeugnisApp.Application.Abstractions;
using ZeugnisApp.Domain.Entities;
using ZeugnisApp.Domain.Enums;

namespace ZeugnisApp.App.ViewModels;

/// <summary>
/// School-wide settings screen (plan §Sprint 1). Autosaves ~1 s after the last edit — the teacher
/// never has to remember to click Save (plan §7.1).
/// </summary>
public sealed partial class EinstellungenViewModel : ViewModelBase
{
    private readonly ISchoolSettingsService _settingsService;
    private readonly ISaveStatusService _saveStatus;
    private readonly IBackupService _backup;
    private readonly IFilePickerService _filePicker;
    private readonly DispatcherTimer _autosaveTimer;
    private bool _loading;

    public EinstellungenViewModel(
        ISchoolSettingsService settingsService, ISaveStatusService saveStatus,
        IBackupService backup, IFilePickerService filePicker)
    {
        _settingsService = settingsService;
        _saveStatus = saveStatus;
        _backup = backup;
        _filePicker = filePicker;
        _autosaveTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _autosaveTimer.Tick += async (_, _) => await FlushAsync();
    }

    [ObservableProperty] private string? _backupStatus;

    [RelayCommand]
    private async Task SicherungErstellenAsync()
    {
        BackupStatus = null;
        var path = _filePicker.PickSaveFile(Strings.Settings_Backup_Create, Strings.Settings_Backup_Filter, Strings.Settings_Backup_DefaultName);
        if (path is null)
        {
            return;
        }

        try
        {
            await _backup.CreateBundleAsync(path);
            BackupStatus = string.Format(CultureInfo.CurrentCulture, Strings.Settings_Backup_Ok, System.IO.Path.GetFileName(path));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Sicherung fehlgeschlagen");
            BackupStatus = string.Format(CultureInfo.CurrentCulture, Strings.Settings_Backup_Error, ex.Message);
        }
    }

    [RelayCommand]
    private async Task SicherungWiederherstellenAsync()
    {
        BackupStatus = null;
        var path = _filePicker.PickFile(Strings.Settings_Backup_Restore, Strings.Settings_Backup_Filter);
        if (path is null)
        {
            return;
        }

        var result = await _backup.StageRestoreAsync(path);
        if (result.Success)
        {
            BackupStatus = Strings.Settings_Backup_RestoreOk;
            System.Windows.Application.Current.Shutdown(0);
        }
        else
        {
            BackupStatus = string.Format(CultureInfo.CurrentCulture, Strings.Settings_Backup_Error, result.Error);
        }
    }

    public string Name { get => _name; set => SetTracked(ref _name, value); }
    private string _name = string.Empty;

    public string Zusatzbezeichnung { get => _zusatzbezeichnung; set => SetTracked(ref _zusatzbezeichnung, value); }
    private string _zusatzbezeichnung = string.Empty;

    public string Adresse { get => _adresse; set => SetTracked(ref _adresse, value); }
    private string _adresse = string.Empty;

    public string Schulleitung { get => _schulleitung; set => SetTracked(ref _schulleitung, value); }
    private string _schulleitung = string.Empty;

    public string Schuljahr { get => _schuljahr; set => SetTracked(ref _schuljahr, value); }
    private string _schuljahr = string.Empty;

    public bool IstErstesHalbjahr { get => _istErstesHalbjahr; set => SetTracked(ref _istErstesHalbjahr, value); }
    private bool _istErstesHalbjahr = true;

    /// <summary>ComboBox binding: 0 = 1. Halbjahr, 1 = 2. Halbjahr.</summary>
    public int HalbjahrIndex
    {
        get => _istErstesHalbjahr ? 0 : 1;
        set => IstErstesHalbjahr = value == 0;
    }

    public DateTime? Zeugnisausgabedatum { get => _zeugnisausgabedatum; set => SetTracked(ref _zeugnisausgabedatum, value); }
    private DateTime? _zeugnisausgabedatum;

    public DateTime? Konferenzdatum { get => _konferenzdatum; set => SetTracked(ref _konferenzdatum, value); }
    private DateTime? _konferenzdatum;

    public DateTime? DatumUnterrichtsWiederbeginn { get => _datumWiederbeginn; set => SetTracked(ref _datumWiederbeginn, value); }
    private DateTime? _datumWiederbeginn;

    public int PapierkorbAufbewahrungTage { get => _papierkorbTage; set => SetTracked(ref _papierkorbTage, value); }
    private int _papierkorbTage = 30;

    public override async Task OnNavigatedToAsync()
    {
        _loading = true;
        try
        {
            var s = await _settingsService.GetAsync();
            _name = s.Name;
            _zusatzbezeichnung = s.Zusatzbezeichnung ?? string.Empty;
            _adresse = s.Adresse;
            _schulleitung = s.Schulleitung;
            _schuljahr = s.Schuljahr;
            _istErstesHalbjahr = s.AktuellesHalbjahr == Halbjahr.Erstes;
            _zeugnisausgabedatum = ToDateTime(s.Zeugnisausgabedatum);
            _konferenzdatum = ToDateTime(s.Konferenzdatum);
            _datumWiederbeginn = ToDateTime(s.DatumUnterrichtsWiederbeginn);
            _papierkorbTage = s.PapierkorbAufbewahrungTage;
            OnPropertyChanged(propertyName: null);
        }
        finally
        {
            _loading = false;
        }
    }

    [RelayCommand]
    private Task SpeichernAsync() => FlushAsync();

    private void SetTracked<T>(ref T field, T value, [System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        OnPropertyChanged(propertyName);

        if (_loading)
        {
            return;
        }

        _saveStatus.MarkDirty();
        _autosaveTimer.Stop();
        _autosaveTimer.Start();
    }

    private async Task FlushAsync()
    {
        _autosaveTimer.Stop();
        if (_loading)
        {
            return;
        }

        try
        {
            var s = await _settingsService.GetAsync();
            s.Name = Name.Trim();
            s.Zusatzbezeichnung = NullIfBlank(Zusatzbezeichnung);
            s.Adresse = Adresse.Trim();
            s.Schulleitung = Schulleitung.Trim();
            s.Schuljahr = Schuljahr.Trim();
            s.AktuellesHalbjahr = IstErstesHalbjahr ? Halbjahr.Erstes : Halbjahr.Zweites;
            s.Zeugnisausgabedatum = ToDateOnly(Zeugnisausgabedatum);
            s.Konferenzdatum = ToDateOnly(Konferenzdatum);
            s.DatumUnterrichtsWiederbeginn = ToDateOnly(DatumUnterrichtsWiederbeginn);
            s.PapierkorbAufbewahrungTage = Math.Clamp(PapierkorbAufbewahrungTage, 1, 3650);

            await _settingsService.SaveAsync(s);
            _saveStatus.MarkSaved();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Schuleinstellungen konnten nicht gespeichert werden");
        }
    }

    private static string? NullIfBlank(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static DateTime? ToDateTime(DateOnly? d) => d is { } v ? v.ToDateTime(TimeOnly.MinValue) : null;
    private static DateOnly? ToDateOnly(DateTime? d) => d is { } v ? DateOnly.FromDateTime(v) : null;
}
