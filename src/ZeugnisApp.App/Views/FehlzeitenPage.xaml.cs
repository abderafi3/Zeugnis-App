using System.Windows.Controls;
using ZeugnisApp.App.ViewModels.Fehlzeiten;

namespace ZeugnisApp.App.Views;

public partial class FehlzeitenPage : Page
{
    public FehlzeitenPage(FehlzeitenViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
