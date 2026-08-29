using Wpf.Ui.Controls;
using ZeugnisApp.App.ViewModels.Formulare;

namespace ZeugnisApp.App.Views.Formulare;

public partial class FeldZuordnungWindow : FluentWindow
{
    public FeldZuordnungWindow(FeldZuordnungViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.RequestClose += confirmed =>
        {
            DialogResult = confirmed;
            Close();
        };
    }
}
