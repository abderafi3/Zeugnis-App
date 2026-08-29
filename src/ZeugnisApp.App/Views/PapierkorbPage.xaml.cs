using System.Windows.Controls;
using ZeugnisApp.App.ViewModels.Papierkorb;

namespace ZeugnisApp.App.Views;

public partial class PapierkorbPage : Page
{
    public PapierkorbPage(PapierkorbViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
