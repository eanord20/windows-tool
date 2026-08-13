using System.Data;
using System.IO;
using System.Text;
using ExcelDataReader;
using NSPdfMerge.App.Models;

namespace NSPdfMerge.App.Services;

public sealed class ExcelImportService
{
    public List<FileRow> ImportRowsFromExcelFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("Не задан путь к Excel файлу.", nameof(filePath));
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("Excel файл не найден.", filePath);
        }

        // ExcelDataReader требует эту регистрацию для старых кодировок (в т.ч. .xls).
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = ExcelReaderFactory.CreateReader(stream);

        var ds = reader.AsDataSet(new ExcelDataSetConfiguration
        {
            ConfigureDataTable = _ => new ExcelDataTableConfiguration
            {
                UseHeaderRow = true
            }
        });

        if (ds.Tables.Count == 0)
        {
            return [];
        }

        // Обычно нужный лист — первый.
        var table = ds.Tables[0];
        if (table.Rows.Count == 0)
        {
            return [];
        }

        int partNumberCol;
        int descriptionCol;

        if (table.Columns.Count >= 3)
        {
            partNumberCol = 1;
            descriptionCol = 2;
        }
        else if (table.Columns.Count >= 2)
        {
            partNumberCol = 0;
            descriptionCol = 1;
        }
        else
        {
            throw new InvalidOperationException("Не удалось определить колонки номера/названия в Excel файле.");
        }

        var rows = new List<FileRow>();

        foreach (DataRow dr in table.Rows)
        {
            var number = dr[partNumberCol]?.ToString()?.Trim() ?? string.Empty;
            var title = dr[descriptionCol]?.ToString()?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(number) && string.IsNullOrWhiteSpace(title))
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
}
