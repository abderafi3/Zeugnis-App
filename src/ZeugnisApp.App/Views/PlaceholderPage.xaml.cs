using System.Windows.Controls;
using ZeugnisApp.App.Resources;

namespace ZeugnisApp.App.Views;

/// <summary>
/// Shell page for a not-yet-built screen. Unused now that every navigation target is real; kept
/// for the next time a screen needs a placeholder during development.
/// </summary>
public partial class PlaceholderPage : Page
{
    public PlaceholderPage(string title, string sprintHinweis)
    {
        InitializeComponent();
        TitleText.Text = title;
        Info.Title = Strings.Placeholder_NotAvailable;
        Info.Message = sprintHinweis;
    }
}
