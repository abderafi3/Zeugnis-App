using System.Windows;
using Wpf.Ui.Appearance;
using ZeugnisApp.Domain.Enums;

namespace ZeugnisApp.App.Services;

/// <summary>Applies the light/dark preference (plan §7.1). "System" follows Windows and keeps following it.</summary>
public static class ThemeApplier
{
    public static void Apply(AppDesign design, Window? window)
    {
        switch (design)
        {
            case AppDesign.Hell:
                Unwatch(window);
                ApplicationThemeManager.Apply(ApplicationTheme.Light);
                break;

            case AppDesign.Dunkel:
                Unwatch(window);
                ApplicationThemeManager.Apply(ApplicationTheme.Dark);
                break;

            default:
                ApplicationThemeManager.Apply(
                    ApplicationThemeManager.GetSystemTheme() == SystemTheme.Dark
                        ? ApplicationTheme.Dark
                        : ApplicationTheme.Light);
                if (window is not null)
                {
                    SystemThemeWatcher.Watch(window);
                }

                break;
        }
    }

    private static void Unwatch(Window? window)
    {
        if (window is not null)
        {
            SystemThemeWatcher.UnWatch(window);
        }
    }
}
