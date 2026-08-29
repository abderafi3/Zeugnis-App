using CommunityToolkit.Mvvm.ComponentModel;
using ZeugnisApp.Application.Abstractions;

namespace ZeugnisApp.App.Services;

/// <summary>
/// Backs the app-wide "Gespeichert um 14:32" indicator (plan §7.1 / §Sprint 1).
/// Any screen that autosaves calls <see cref="MarkSaved"/>; the shell status bar binds to this.
/// </summary>
public interface ISaveStatusService
{
    void MarkSaved();
    void MarkDirty();
}

public sealed partial class SaveStatusService(IClock clock) : ObservableObject, ISaveStatusService
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusText))]
    private DateTime? _lastSavedUtc;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusText))]
    private bool _hasUnsavedChanges;

    public string StatusText =>
        HasUnsavedChanges ? "Nicht gespeicherte Änderungen …"
        : LastSavedUtc is { } utc ? $"Gespeichert um {utc.ToLocalTime():HH:mm}"
        : "Automatisches Speichern aktiv";

    public void MarkSaved()
    {
        LastSavedUtc = clock.UtcNow;
        HasUnsavedChanges = false;
    }

    public void MarkDirty() => HasUnsavedChanges = true;
}
