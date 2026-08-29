using Wpf.Ui.Controls;
using ZeugnisApp.App.ViewModels.Floskeln;

namespace ZeugnisApp.App.Views.Floskeln;

public partial class FloskelPickerWindow : FluentWindow
{
    public FloskelPickerWindow(FloskelPickerViewModel viewModel)
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
