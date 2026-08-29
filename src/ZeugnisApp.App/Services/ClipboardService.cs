using System.Windows;

namespace ZeugnisApp.App.Services;

/// <summary>Read-only clipboard text access, abstracted for testability.</summary>
public interface IClipboardService
{
    string? GetText();
}

public sealed class ClipboardService : IClipboardService
{
    public string? GetText()
        => Clipboard.ContainsText() ? Clipboard.GetText() : null;
}
