using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;

namespace NSPdfMerge.App.Converters;

public sealed class RowIndexConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (parameter is not DataGrid dataGrid || value is null)
            return string.Empty;

        var index = dataGrid.Items.IndexOf(value);
        return index < 0 ? string.Empty : (index + 1).ToString();
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
