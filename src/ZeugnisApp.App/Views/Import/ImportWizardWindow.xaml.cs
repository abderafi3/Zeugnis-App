using Wpf.Ui.Controls;
using ZeugnisApp.App.ViewModels.Import;

namespace ZeugnisApp.App.Views.Import;

public partial class ImportWizardWindow : FluentWindow
{
    public ImportWizardWindow(ImportWizardViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
