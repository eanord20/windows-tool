using System.Windows;

namespace NSPdfMerge.App;

public partial class InputBoxWindow : Window
{
    public string Text { get; set; } = string.Empty;

    public InputBoxWindow(string title, string defaultText)
    {
        Title = title;
        Text = defaultText;
        InitializeComponent();
        DataContext = this;
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }
}
