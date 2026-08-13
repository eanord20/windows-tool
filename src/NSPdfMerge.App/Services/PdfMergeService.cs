using System.IO;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using NSPdfMerge.App.Models;

namespace NSPdfMerge.App.Services;

public sealed class PdfMergeService
{
    public MergeResult MergeToFile(IEnumerable<FileRow> rows, string outputPdfPath)
    {
        if (string.IsNullOrWhiteSpace(outputPdfPath))
        {
            throw new InvalidOperationException("Не задан путь для сохранения итогового PDF.");
        }

        var outputDir = Path.GetDirectoryName(outputPdfPath);
        if (string.IsNullOrWhiteSpace(outputDir))
        {
            throw new InvalidOperationException("Не удалось определить папку для сохранения итогового PDF.");
        }

        Directory.CreateDirectory(outputDir);

        var result = new MergeResult();

        using var output = new PdfDocument();
        output.Info.Title = "NSPdfMerge Result";

        // Convert to list to ensure stable ordering and add debug info
        var rowsList = rows.ToList();
        AppLog.Info($"Processing {rowsList.Count} rows in order for merge");

        for (int rowIndex = 0; rowIndex < rowsList.Count; rowIndex++)
        {
            var row = rowsList[rowIndex];
            
            AppLog.Info($"Processing row {rowIndex + 1}: Number={row.Number}, Title={row.Title}, Include={row.Include}, Status={row.Status}, Path={row.ResolvedPath}");

            if (!row.Include)
            {
                AppLog.Info($"Row {rowIndex + 1} skipped (not included)");
                result.SkippedNotIncluded++;
                continue;
            }

            if (row.Status == ResolveStatus.Found && File.Exists(row.ResolvedPath))
            {
                try
                {
                    using var input = PdfReader.Open(row.ResolvedPath, PdfDocumentOpenMode.Import);
                    for (var i = 0; i < input.PageCount; i++)
                    {
                        output.AddPage(input.Pages[i]);
                    }

                    AppLog.Info($"Row {rowIndex + 1} added: {input.PageCount} pages from {row.ResolvedPath}");
                    result.AddedFiles++;
                    result.AddedPages += input.PageCount;
                }
                catch (Exception ex)
                {
                    AppLog.Error($"Failed to add PDF for row {rowIndex + 1}: {ex.Message}");
                    result.Errors++;
                    AddPlaceholderPage(output, row, $"Ошибка чтения PDF: {ex.Message}");
                }

                continue;
            }

            // NotFound / Ambiguous / InvalidPath / Pending -> заглушка
            AppLog.Info($"Row {rowIndex + 1} placeholder: {row.Status}");
            result.Placeholders++;
            var reason = row.Status switch
            {
                ResolveStatus.NotFound => "Файл не найден",
                ResolveStatus.Ambiguous => "Несколько совпадений (нужен выбор)",
                ResolveStatus.InvalidPath => "Некорректный путь",
                ResolveStatus.Pending => "Не выполнялся поиск",
                _ => "Неизвестно"
            };

            AddPlaceholderPage(output, row, reason);
        }

        output.Save(outputPdfPath);
        result.OutputPath = outputPdfPath;

        AppLog.Info($"Merge completed: AddedFiles={result.AddedFiles}, AddedPages={result.AddedPages}, Placeholders={result.Placeholders}, Errors={result.Errors}, Skipped={result.SkippedNotIncluded}");

        return result;
    }

    private static void AddPlaceholderPage(PdfDocument output, FileRow row, string reason)
    {
        var page = output.AddPage();
        page.Size = PdfSharp.PageSize.A4;

        using var gfx = XGraphics.FromPdfPage(page);
        var titleFont = CreateFontSafe(18, XFontStyleEx.Bold);
        var textFont = CreateFontSafe(12, XFontStyleEx.Regular);

        var margin = 48;
        var y = margin;
        var pageWidth = page.Width.Point;
        var contentWidth = Math.Max(0, pageWidth - margin * 2);

        gfx.DrawString("Файл не добавлен", titleFont, XBrushes.DarkRed, new XRect(margin, y, contentWidth, 40), XStringFormats.TopLeft);
        y += 44;

        gfx.DrawString($"Причина: {reason}", textFont, XBrushes.Black, new XRect(margin, y, contentWidth, 24), XStringFormats.TopLeft);
        y += 28;

        gfx.DrawString($"Номер: {row.Number}", textFont, XBrushes.Black, new XRect(margin, y, contentWidth, 24), XStringFormats.TopLeft);
        y += 20;

        gfx.DrawString($"Название: {row.Title}", textFont, XBrushes.Black, new XRect(margin, y, contentWidth, 60), XStringFormats.TopLeft);
        y += 44;

        gfx.DrawString($"Путь строки: {row.RowPath}", textFont, XBrushes.Gray, new XRect(margin, y, contentWidth, 40), XStringFormats.TopLeft);
        y += 36;

        gfx.DrawString($"ResolvedPath: {row.ResolvedPath}", textFont, XBrushes.Gray, new XRect(margin, y, contentWidth, 40), XStringFormats.TopLeft);
        y += 36;

        gfx.DrawString($"Дата: {DateTime.Now:yyyy-MM-dd HH:mm:ss}", textFont, XBrushes.Black, new XRect(margin, y, contentWidth, 24), XStringFormats.TopLeft);
    }

    private static XFont CreateFontSafe(double size, XFontStyleEx style)
    {
        // PDFsharp 6 may fail to resolve some fonts depending on installed fonts.
        // Try a few common families.
        var families = new[] { "Arial", "Calibri", "Segoe UI", "Times New Roman" };
        foreach (var family in families)
        {
            try
            {
                return new XFont(family, size, style);
            }
            catch
            {
                // ignore and try next
            }
        }

        // As a last resort, try with default family name.
        return new XFont("Arial", size, style);
    }
}

public sealed class MergeResult
{
    public string OutputPath { get; set; } = string.Empty;
    public int AddedFiles { get; set; }
    public int AddedPages { get; set; }
    public int Placeholders { get; set; }
    public int Errors { get; set; }
    public int SkippedNotIncluded { get; set; }
}
