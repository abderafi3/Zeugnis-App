using System.Windows;
using System.Windows.Controls;
using ZeugnisApp.App.ViewModels.Klassen;

namespace ZeugnisApp.App.Views.Klassen;

public partial class KinderImportView : UserControl
{
    public KinderImportView() => InitializeComponent();

    private void OnDragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void OnFileDrop(object sender, DragEventArgs e)
    {
        if (DataContext is not KinderImportViewModel vm)
        {
            return;
        }

        if (e.Data.GetData(DataFormats.FileDrop) is string[] { Length: > 0 } files)
        {
            vm.LoadDroppedFile(files[0]);
        }
    }
}
