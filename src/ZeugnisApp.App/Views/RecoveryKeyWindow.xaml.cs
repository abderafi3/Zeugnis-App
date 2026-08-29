using System.Windows;
using Wpf.Ui.Controls;
using ZeugnisApp.App.Resources;

namespace ZeugnisApp.App.Views;

public partial class RecoveryKeyWindow : FluentWindow
{
    private readonly bool _displayMode;

    /// <param name="displayKey">non-null → show this key once; null → prompt the teacher for it.</param>
    public RecoveryKeyWindow(string? displayKey)
    {
        InitializeComponent();
        _displayMode = displayKey is not null;

        if (_displayMode)
        {
            IntroText.Text = Strings.Recovery_ShowIntro;
            KeyBox.Text = displayKey;
            KeyBox.IsReadOnly = true;
            CopyButton.Visibility = Visibility.Visible;
            CancelButton.Visibility = Visibility.Collapsed;
        }
        else
        {
            IntroText.Text = Strings.Recovery_EnterIntro;
            OkButton.Content = Strings.Recovery_Unlock;
        }
    }

    public string EnteredKey { get; private set; } = string.Empty;

    public void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorText.Visibility = Visibility.Visible;
    }

    private void OnCopy(object sender, RoutedEventArgs e)
    {
        try { Clipboard.SetText(KeyBox.Text); } catch (System.Runtime.InteropServices.COMException) { /* clipboard busy */ }
    }

    private void OnOk(object sender, RoutedEventArgs e)
    {
        if (!_displayMode && string.IsNullOrWhiteSpace(KeyBox.Text))
        {
            ShowError(Strings.Recovery_Empty);
            return;
        }

        EnteredKey = KeyBox.Text.Trim();
        DialogResult = true;
        Close();
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
