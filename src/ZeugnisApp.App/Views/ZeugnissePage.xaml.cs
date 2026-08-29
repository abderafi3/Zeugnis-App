using System.Windows.Controls;
using ZeugnisApp.App.ViewModels.Zeugnisse;

namespace ZeugnisApp.App.Views;

public partial class ZeugnissePage : Page
{
    public ZeugnissePage(ZeugnisseWorkspaceViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
