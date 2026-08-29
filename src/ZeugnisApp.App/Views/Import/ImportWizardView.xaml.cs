using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using ZeugnisApp.App.Resources;
using ZeugnisApp.App.ViewModels.Import;

namespace ZeugnisApp.App.Views.Import;

public partial class ImportWizardView : UserControl
{
    public ImportWizardView() => InitializeComponent();

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is ImportWizardViewModel vm)
        {
            BuildPreviewColumns(vm);
        }
    }

    /// <summary>Preview-grid columns come from the active <see cref="ImportProfile"/>, not hard-coded.</summary>
    private void BuildPreviewColumns(ImportWizardViewModel vm)
    {
        PreviewGrid.Columns.Clear();

        PreviewGrid.Columns.Add(new DataGridCheckBoxColumn
        {
            Header = Strings.Import_Col_Include,
            Binding = new Binding(nameof(ImportRowViewModel.Included)) { UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged },
        });

        PreviewGrid.Columns.Add(new DataGridTextColumn
        {
            Header = Strings.Import_Col_SourceRow,
            IsReadOnly = true,
            Binding = new Binding(nameof(ImportRowViewModel.SourceRowNumber)),
        });

        for (var i = 0; i < vm.Profile.Fields.Count; i++)
        {
            PreviewGrid.Columns.Add(new DataGridTextColumn
            {
                Header = vm.Profile.Fields[i].DisplayName,
                Width = new DataGridLength(1, DataGridLengthUnitType.Star),
                Binding = new Binding($"Cells[{i}].Value") { UpdateSourceTrigger = UpdateSourceTrigger.LostFocus },
            });
        }

        PreviewGrid.Columns.Add(new DataGridTextColumn
        {
            Header = Strings.Import_Col_Errors,
            IsReadOnly = true,
            Width = new DataGridLength(2, DataGridLengthUnitType.Star),
            Binding = new Binding(nameof(ImportRowViewModel.ErrorText)),
        });
    }

    private void OnDragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void OnFileDrop(object sender, DragEventArgs e)
    {
        if (DataContext is ImportWizardViewModel vm
            && e.Data.GetData(DataFormats.FileDrop) is string[] { Length: > 0 } files)
        {
            vm.LoadDroppedFile(files[0]);
        }
    }
}
