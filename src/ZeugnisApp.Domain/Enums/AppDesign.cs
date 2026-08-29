namespace ZeugnisApp.Domain.Enums;

/// <summary>Light/dark preference for the UI (plan §7.1). Per-device, stored on the settings row.</summary>
public enum AppDesign
{
    /// <summary>Follow the Windows setting.</summary>
    System = 0,
    Hell = 1,
    Dunkel = 2,
}
