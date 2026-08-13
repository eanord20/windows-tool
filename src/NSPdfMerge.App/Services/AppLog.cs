using System.Globalization;
using System.IO;

namespace NSPdfMerge.App.Services;

public static class AppLog
{
    private static readonly object _sync = new();

    public static string LogFilePath { get; } = InitLogFilePath();

    public static void Info(string message) => Write("INFO", message);

    public static void Error(string message, Exception? ex = null)
        => Write("ERROR", ex is null ? message : $"{message}\n{ex}");

    private static void Write(string level, string message)
    {
        try
        {
            lock (_sync)
            {
                var ts = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
                File.AppendAllText(LogFilePath, $"[{ts}] {level}: {message}{Environment.NewLine}");
            }
        }
        catch
        {
            // no-op: logging must never crash the app
        }
    }

    private static string InitLogFilePath()
    {
        var baseDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "NSPdfMerge",
            "logs");

        Directory.CreateDirectory(baseDir);

        var fileName = $"app-{DateTime.Now:yyyyMMdd}.log";
        return Path.Combine(baseDir, fileName);
    }
}
