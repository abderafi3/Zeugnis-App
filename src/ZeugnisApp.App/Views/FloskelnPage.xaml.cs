using System.Windows.Controls;
using ZeugnisApp.App.ViewModels.Floskeln;

namespace ZeugnisApp.App.Views;

public partial class FloskelnPage : Page
{
    public FloskelnPage(FloskelnViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
