using System.Windows;

namespace NSPdfMerge.App;

public partial class UpdateWindow : Window
{
    public UpdateWindow()
    {
        InitializeComponent();
        DataContext = new UpdateViewModel();
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
