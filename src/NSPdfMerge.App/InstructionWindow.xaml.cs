using System.Windows;

namespace NSPdfMerge.App;

public partial class InstructionWindow : Window
{
    public InstructionWindow(string text)
    {
        InitializeComponent();
        DataContext = text;
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
