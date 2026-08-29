using ZeugnisApp.App.Resources;
using WpfUiMessageBox = Wpf.Ui.Controls.MessageBox;

namespace ZeugnisApp.App.Services;

/// <summary>Modal confirmations, abstracted so view models stay UI-free and testable.</summary>
public interface IDialogService
{
    Task<bool> ConfirmAsync(string title, string body, string? okText = null, string? cancelText = null);
}

public sealed class DialogService : IDialogService
{
    public async Task<bool> ConfirmAsync(string title, string body, string? okText = null, string? cancelText = null)
    {
        var dialog = new WpfUiMessageBox
        {
            Title = title,
            Content = body,
            PrimaryButtonText = okText ?? Strings.Common_Delete,
            CloseButtonText = cancelText ?? Strings.Common_Cancel,
        };

        var result = await dialog.ShowDialogAsync();
        return result == Wpf.Ui.Controls.MessageBoxResult.Primary;
    }
}
