using System.Text;

namespace NSPdfMerge.App.Services;

public static class NameNormalize
{
    public static string BuildExpectedFull(string number, string title)
    {
        var combined = string.IsNullOrWhiteSpace(title)
            ? number
            : $"{number} {title}";

        return NormalizeFileBaseName(combined);
    }

    public static string NormalizeNumber(string number)
    {
        return NormalizeFileBaseName(number);
    }

    public static string NormalizeFileBaseName(string input)
    {
        // Правило:
        // - регистр не важен
        // - запрещённые символы Windows игнорируем
        // - пробелы/_/- приводим к одному пробелу
        // - множественные пробелы схлопываем

        if (string.IsNullOrWhiteSpace(input)) return string.Empty;

        var sb = new StringBuilder();
        bool prevWasSpace = false;

        foreach (var ch in input.Trim().ToLowerInvariant())
        {
            if (IsForbiddenWindowsChar(ch))
            {
                continue;
            }

            if (ch == '_' || ch == '-' || char.IsWhiteSpace(ch))
            {
                if (!prevWasSpace)
                {
                    sb.Append(' ');
                    prevWasSpace = true;
                }
                continue;
            }

            sb.Append(ch);
            prevWasSpace = false;
        }

        return sb.ToString().Trim();
    }

    private static bool IsForbiddenWindowsChar(char ch)
        => ch is '<' or '>' or ':' or '"' or '/' or '\\' or '|' or '?' or '*';
}
