using System.Windows.Controls;

namespace ZeugnisApp.App.Views;

/// <summary>
/// Temporary shell page for screens that land in a later sprint. Keeps the five-area
/// navigation walkable from Sprint 0 (plan §Sprint 0 DoD).
/// </summary>
public partial class PlaceholderPage : Page
{
    public PlaceholderPage(string title, string sprintHinweis)
    {
        InitializeComponent();
        TitleText.Text = title;
        Info.Message = sprintHinweis;
    }
}

// One concrete type per navigation target (WPF-UI resolves pages by CLR type).

public sealed class KlassenPage : PlaceholderPage
{
    public KlassenPage() : base("Klassen", "Klassen- und Kinderverwaltung entsteht in Sprint 2.") { }
}

public sealed class ZeugnissePage : PlaceholderPage
{
    public ZeugnissePage() : base("Zeugnisse", "Der Zeugnis-Editor entsteht in Sprint 8, die Erzeugung in Sprint 9.") { }
}

public sealed class FormularePage : PlaceholderPage
{
    public FormularePage() : base("Formulare", "Formularverwaltung entsteht in Sprint 5.") { }
}

public sealed class FloskelnPage : PlaceholderPage
{
    public FloskelnPage() : base("Floskeln", "Die Floskeln-Bibliothek entsteht in Sprint 4.") { }
}

public sealed class FehlzeitenPage : PlaceholderPage
{
    public FehlzeitenPage() : base("Fehlzeiten", "Die Fehlzeiten-Tabelle entsteht in Sprint 7.") { }
}
