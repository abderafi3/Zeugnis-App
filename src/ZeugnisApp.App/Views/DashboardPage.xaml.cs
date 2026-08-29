using System.Windows.Controls;
using ZeugnisApp.App.ViewModels;

namespace ZeugnisApp.App.Views;

public partial class DashboardPage : Page
{
    public DashboardPage(DashboardViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
