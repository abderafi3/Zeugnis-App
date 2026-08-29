using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Wpf.Ui;

namespace ZeugnisApp.App.Services;

/// <summary>
/// Resolves navigation target pages from the DI container so every page gets its
/// constructor-injected view model (plan §3 — no business logic in views).
/// </summary>
public sealed class PageService(IServiceProvider serviceProvider) : IPageService
{
    public T? GetPage<T>() where T : class
        => serviceProvider.GetService<T>();

    public FrameworkElement? GetPage(Type pageType)
    {
        if (!typeof(FrameworkElement).IsAssignableFrom(pageType))
        {
            throw new InvalidOperationException($"{pageType.FullName} ist kein FrameworkElement.");
        }

        return serviceProvider.GetService(pageType) as FrameworkElement;
    }
}
