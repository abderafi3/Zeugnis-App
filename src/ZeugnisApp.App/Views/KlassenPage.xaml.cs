using System.Windows.Controls;
using ZeugnisApp.App.ViewModels.Klassen;

namespace ZeugnisApp.App.Views;

public partial class KlassenPage : Page
{
    public KlassenPage(KlassenWorkspaceViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
