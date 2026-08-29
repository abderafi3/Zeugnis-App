using System.Windows.Controls;
using ZeugnisApp.App.Resources;

namespace ZeugnisApp.App.Views;

/// <summary>
/// Temporary shell page for screens that land in a later sprint. Keeps the
/// navigation walkable from Sprint 0 (plan §Sprint 0 DoD).
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

// One concrete type per navigation target (WPF-UI resolves pages by CLR type).

public sealed class ZeugnissePage() : PlaceholderPage(Strings.Nav_Zeugnisse, Strings.Placeholder_Zeugnisse);
