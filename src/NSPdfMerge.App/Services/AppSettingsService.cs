using System.IO;
using System.Text.Json;

namespace NSPdfMerge.App.Services;

public sealed class AppSettingsService
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public string SettingsFilePath { get; }

    public AppSettingsService()
    {
        var baseDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "NSPdfMerge");

        Directory.CreateDirectory(baseDir);
        SettingsFilePath = Path.Combine(baseDir, "settings.json");
    }

    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsFilePath)) return new AppSettings();

            var json = File.ReadAllText(SettingsFilePath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, _jsonOptions);
            return settings ?? new AppSettings();
        }
        catch (Exception ex)
        {
            AppLog.Error("Failed to load settings", ex);
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        try
        {
            var json = JsonSerializer.Serialize(settings, _jsonOptions);
            File.WriteAllText(SettingsFilePath, json);
        }
        catch (Exception ex)
        {
            AppLog.Error("Failed to save settings", ex);
        }
    }
}
