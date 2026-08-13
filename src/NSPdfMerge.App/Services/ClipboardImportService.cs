using System.Text;
using NSPdfMerge.App.Models;

namespace NSPdfMerge.App.Services;

public sealed class ClipboardImportService
{
    public List<FileRow> ImportRowsFromClipboardTsv()
    {
        var text = System.Windows.Clipboard.GetText();
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new InvalidOperationException("Буфер обмена пуст или не содержит текста.");
        }

        // Excel copy обычно даёт TSV с \r\n и \t.
        var lines = text
            .Replace("\r\n", "\n")
            .Replace('\r', '\n')
            .Split('\n', StringSplitOptions.RemoveEmptyEntries);

        var rows = new List<FileRow>();

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (line.Length == 0) continue;

            var parts = line.Split('\t');
            if (parts.Length < 1) continue;

            var number = parts[0].Trim();
            var title = parts.Length >= 2 ? parts[1].Trim() : string.Empty;

            // Игнорируем заголовок, если он похож на "номер"/"название".
            if (rows.Count == 0 && IsHeaderRow(number, title))
            {
                continue;
            }

            rows.Add(new FileRow
            {
                Include = true,
                Number = number,
                Title = title,
                RowPath = string.Empty,
                ResolvedPath = string.Empty,
                Status = ResolveStatus.Pending
            });
        }

        return rows;
    }

    private static bool IsHeaderRow(string number, string title)
    {
        var n = Normalize(number);
        var t = Normalize(title);

        return (n.Contains("номер") || n.Contains("no") || n.Contains("num"))
               && (t.Contains("название") || t.Contains("name") || t.Contains("title"));
    }

    private static string Normalize(string s)
    {
        var sb = new StringBuilder();
        foreach (var ch in s.Trim().ToLowerInvariant())
        {
            if (char.IsWhiteSpace(ch))
            {
                sb.Append(' ');
                continue;
            }

            sb.Append(ch);
        }

        return sb.ToString();
    }
}
