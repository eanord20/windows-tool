using System.Windows;

namespace NSPdfMerge.App;

public partial class AboutWindow : Window
{
    public AboutWindow(string text)
    {
        InitializeComponent();
        DataContext = text;
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
