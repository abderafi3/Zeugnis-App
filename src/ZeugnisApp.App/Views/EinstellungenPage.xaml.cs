using System.Windows.Controls;
using ZeugnisApp.App.ViewModels;

namespace ZeugnisApp.App.Views;

public partial class EinstellungenPage : Page
{
    public EinstellungenPage(EinstellungenViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
