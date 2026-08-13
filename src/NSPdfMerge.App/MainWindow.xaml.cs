using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using NSPdfMerge.App.Models;
using NSPdfMerge.App.ViewModels;

namespace NSPdfMerge.App;

public partial class MainWindow : Window
{
    private System.Windows.Point _dragStartPoint;

    public MainWindow()
    {
        InitializeComponent();
    }

    private void RowsGrid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragStartPoint = e.GetPosition(null);
    }

    private void RowsGrid_PreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed) return;

        var position = e.GetPosition(null);
        var diff = _dragStartPoint - position;

        if (Math.Abs(diff.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(diff.Y) < SystemParameters.MinimumVerticalDragDistance)
        {
            return;
        }

        // Do not initiate row drag if the user is selecting text inside a TextBox cell,
        // resizing/reordering a column, or interacting with the column header.
        if (e.OriginalSource is System.Windows.Controls.TextBox) return;
        if (FindVisualParent<DataGridRow>((DependencyObject)e.OriginalSource) is null) return;

        if (sender is not DataGrid dataGrid) return;
        var draggedRows = dataGrid.SelectedItems.Cast<FileRow>().ToList();
        if (draggedRows.Count == 0) return;

        DragDrop.DoDragDrop(dataGrid, draggedRows, System.Windows.DragDropEffects.Move);
    }

    private void RowsGrid_Drop(object sender, System.Windows.DragEventArgs e)
    {
        if (sender is not DataGrid dataGrid) return;

        if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
        {
            HandleFileDrop(e);
            return;
        }

        if (!e.Data.GetDataPresent(typeof(List<FileRow>))) return;

        var droppedRows = e.Data.GetData(typeof(List<FileRow>)) as List<FileRow>;
        if (droppedRows is null || droppedRows.Count == 0) return;

        if (DataContext is not MainViewModel vm) return;

        var targetRow = FindVisualParent<DataGridRow>((DependencyObject)e.OriginalSource);
        var targetData = targetRow?.Item as FileRow;

        // snapshot handled here because code-behind initiates the change
        vm.PushUndoSnapshot();

        var orderedToMove = droppedRows
            .Select(r => (Row: r, Index: vm.Rows.IndexOf(r)))
            .Where(x => x.Index >= 0)
            .OrderByDescending(x => x.Index)
            .ToList();

        if (orderedToMove.Count == 0) return;

        var targetIndex = targetData is null ? vm.Rows.Count : vm.Rows.IndexOf(targetData);
        if (targetIndex < 0) targetIndex = vm.Rows.Count;

        // Remove rows in reverse order to keep indices valid.
        foreach (var item in orderedToMove)
        {
            vm.Rows.RemoveAt(item.Index);
        }

        // Adjust insertion index for rows that were above the target.
        var insertIndex = targetIndex;
        foreach (var item in orderedToMove)
        {
            if (item.Index < targetIndex)
                insertIndex--;
        }

        // Insert rows in original ascending order to preserve order.
        var ascending = orderedToMove.OrderBy(x => x.Index).Select(x => x.Row).ToList();
        foreach (var row in ascending)
        {
            if (insertIndex < 0) insertIndex = 0;
            if (insertIndex > vm.Rows.Count) insertIndex = vm.Rows.Count;
            vm.Rows.Insert(insertIndex++, row);
        }

        vm.SelectedRow = ascending.FirstOrDefault();
    }

    private void RowsGrid_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        // Ensure the row under cursor becomes selected so ContextMenu->Delete is intuitive.
        var row = FindVisualParent<DataGridRow>((DependencyObject)e.OriginalSource);
        if (row is null) return;

        if (sender is DataGrid grid)
        {
            grid.SelectedItem = row.Item;
        }

        row.IsSelected = true;
        row.Focus();
    }

    private void RowsGrid_DragOver(object sender, System.Windows.DragEventArgs e)
    {
        if (sender is not DataGrid dataGrid) return;
        if (!e.Data.GetDataPresent(typeof(List<FileRow>)) && !e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
        {
            return;
        }

        var targetRow = FindVisualParent<DataGridRow>((DependencyObject)e.OriginalSource);
        if (targetRow?.Item is FileRow target)
        {
            dataGrid.SelectedItem = target;
        }

        e.Effects = System.Windows.DragDropEffects.Move;
        e.Handled = true;
    }

    private void RowsGrid_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key != Key.Delete) return;

        // Let TextBox keep Delete for editing.
        if (e.OriginalSource is System.Windows.Controls.TextBox) return;

        if (sender is not DataGrid dataGrid) return;
        if (DataContext is not MainViewModel vm) return;
        if (dataGrid.SelectedItems.Count == 0) return;

        if (vm.DeleteSelectedRowsCommand.CanExecute(dataGrid.SelectedItems))
        {
            vm.DeleteSelectedRowsCommand.Execute(dataGrid.SelectedItems);
            e.Handled = true;
        }
    }

    private void HandleFileDrop(System.Windows.DragEventArgs e)
    {
        if (DataContext is not MainViewModel vm) return;

        var files = e.Data.GetData(System.Windows.DataFormats.FileDrop) as string[];
        if (files is null || files.Length == 0) return;

        var pdfFiles = files
            .Where(f => f.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (pdfFiles.Count == 0) return;

        vm.PushUndoSnapshot();
        foreach (var file in pdfFiles)
        {
            var row = new NSPdfMerge.App.Models.FileRow
            {
                Include = true,
                Number = System.IO.Path.GetFileNameWithoutExtension(file),
                Title = string.Empty,
                RowPath = string.Empty,
                ResolvedPath = file,
                Status = NSPdfMerge.App.Models.ResolveStatus.Found,
                IsManuallyResolved = true
            };
            vm.Rows.Add(row);
        }

        vm.SelectedRow = vm.Rows.LastOrDefault();
        vm.AppendLog($"Добавлено файлов из проводника: {pdfFiles.Count}\r\n");
    }

    private static T? FindVisualParent<T>(DependencyObject child) where T : DependencyObject
    {
        DependencyObject? parentObject = child;
        while (parentObject is not null)
        {
            if (parentObject is T parent) return parent;
            parentObject = VisualTreeHelper.GetParent(parentObject);
        }

        return null;
    }
}
