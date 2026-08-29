using System.Windows.Controls;
using ZeugnisApp.App.ViewModels.Formulare;

namespace ZeugnisApp.App.Views;

public partial class FormularePage : Page
{
    public FormularePage(FormulareWorkspaceViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
