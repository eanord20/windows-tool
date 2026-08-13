using System.Windows;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;

namespace NSPdfMerge.App.Services;

public sealed class ThemeService
{
    private readonly PaletteHelper _paletteHelper = new();

    public void ApplyTheme(bool isDark)
    {
        var theme = _paletteHelper.GetTheme();
        theme.SetBaseTheme(isDark ? Theme.Dark : Theme.Light);
        _paletteHelper.SetTheme(theme);

        var brush = new SolidColorBrush(isDark ? System.Windows.Media.Color.FromRgb(0xF0, 0xF0, 0xF0) : System.Windows.Media.Colors.Black);
        if (System.Windows.Application.Current != null)
        {
            System.Windows.Application.Current.Resources["TableForegroundBrush"] = brush;
        }
    }
}
