using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace NSPdfMerge.App;

public partial class AmbiguousSelectWindow : Window
{
    public sealed class Vm
    {
        public ObservableCollection<string> Candidates { get; }
        public string? SelectedCandidate { get; set; }

        public Vm(IEnumerable<string> candidates)
        {
            Candidates = new ObservableCollection<string>(candidates);
            SelectedCandidate = Candidates.FirstOrDefault();
        }
    }

    public string? SelectedPath => (DataContext as Vm)?.SelectedCandidate;

    public AmbiguousSelectWindow(IEnumerable<string> candidates)
    {
        InitializeComponent();
        DataContext = new Vm(candidates);
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
