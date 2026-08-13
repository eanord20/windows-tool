using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using NSPdfMerge.App.Models;

namespace NSPdfMerge.App.Services;

public sealed class FileResolveService
{
    public string ResolveAll(ObservableCollection<FileRow> rows, string commonSearchPath)
    {
        var sb = new StringBuilder();

        if (rows.Count == 0)
        {
            return "Нет строк для поиска.\r\n";
        }

        foreach (var row in rows)
        {
            ResolveOne(row, commonSearchPath, sb);
        }

        return sb.ToString();
    }

    public List<string> GetCandidates(FileRow row, string commonSearchPath)
    {
        var scopePath = string.IsNullOrWhiteSpace(row.RowPath) ? commonSearchPath : row.RowPath;
        if (string.IsNullOrWhiteSpace(scopePath))
        {
            return [];
        }

        return FindCandidates(scopePath, row.Number, row.Title);
    }

    private void ResolveOne(FileRow row, string commonSearchPath, StringBuilder log)
    {
        if (row.IsManuallyResolved)
        {
            log.AppendLine($"[{row.Number}] Пропущен: файл выбран вручную.");
            return;
        }

        row.ResolvedPath = string.Empty;
        row.Status = ResolveStatus.Pending;
        row.Candidates = [];

        var scopePath = string.IsNullOrWhiteSpace(row.RowPath) ? commonSearchPath : row.RowPath;
        if (string.IsNullOrWhiteSpace(scopePath))
        {
            row.Status = ResolveStatus.InvalidPath;
            log.AppendLine($"[{row.Number}] Не задан путь поиска (общий или для строки).");
            return;
        }

        var candidates = FindCandidates(scopePath, row.Number, row.Title);
        row.Candidates = candidates;
        if (candidates.Count == 0)
        {
            row.Status = ResolveStatus.NotFound;
            row.ResolvedPath = string.Empty;
            AppLog.Info($"[{row.Number}] NotFound: {row.Number} {row.Title}");
            log.AppendLine($"[{row.Number}] Не найдено: {row.Number} {row.Title}");
            return;
        }

        if (candidates.Count == 1)
        {
            row.Status = ResolveStatus.Found;
            row.ResolvedPath = candidates[0];
            row.Candidates = [];
            AppLog.Info($"[{row.Number}] Found: {candidates[0]}");
            return;
        }

        row.Status = ResolveStatus.Ambiguous;
        row.ResolvedPath = string.Empty;
        AppLog.Info($"[{row.Number}] Ambiguous: {candidates.Count} candidates");
        log.AppendLine($"[{row.Number}] Несколько совпадений: {candidates.Count}");
    }

    private static List<string> FindCandidates(string scopePath, string number, string title)
    {
        // TODO:
        // 1) если scopePath - файл .pdf -> вернуть его
        // 2) если папка -> рекурсивно найти .pdf
        // 3) сопоставить по нормализованному точному имени: number + " " + title
        // 4) fallback: по префиксу number

        var candidates = new List<string>();

        if (File.Exists(scopePath) && scopePath.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            candidates.Add(scopePath);
            return candidates;
        }

        if (!Directory.Exists(scopePath))
        {
            return candidates;
        }

        var expectedFull = NameNormalize.BuildExpectedFull(number, title);
        var expectedNumber = NameNormalize.NormalizeNumber(number);

        foreach (var file in Directory.EnumerateFiles(scopePath, "*.pdf", SearchOption.AllDirectories))
        {
            var baseName = Path.GetFileNameWithoutExtension(file);
            var normalized = NameNormalize.NormalizeFileBaseName(baseName);

            if (normalized == expectedFull)
            {
                candidates.Add(file);
            }
        }

        if (candidates.Count > 0)
        {
            return candidates;
        }

        // fallback по номеру
        if (!string.IsNullOrWhiteSpace(expectedNumber))
        {
            foreach (var file in Directory.EnumerateFiles(scopePath, "*.pdf", SearchOption.AllDirectories))
            {
                var baseName = Path.GetFileNameWithoutExtension(file);
                var normalized = NameNormalize.NormalizeFileBaseName(baseName);

                if (normalized.StartsWith(expectedNumber, StringComparison.OrdinalIgnoreCase))
                {
                    candidates.Add(file);
                }
            }
        }

        return candidates;
    }
}
