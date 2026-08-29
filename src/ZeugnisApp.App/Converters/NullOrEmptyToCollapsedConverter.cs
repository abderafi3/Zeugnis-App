using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ZeugnisApp.App.Converters;

/// <summary>
/// null / whitespace-only string → <see cref="Visibility.Collapsed"/>; anything else → Visible.
/// Used to show inline validation messages only when there is one.
/// </summary>
public sealed class NullOrEmptyToCollapsedConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => string.IsNullOrWhiteSpace(value as string) ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
