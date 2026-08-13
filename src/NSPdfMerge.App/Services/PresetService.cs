using System.IO;
using System.Text.Json;

namespace NSPdfMerge.App.Services;

public sealed class PresetsData
{
    public List<PathPreset> SearchPathPresets { get; set; } = new();
    public List<PathPreset> OutputPathPresets { get; set; } = new();
}

public sealed class PresetService
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public string PresetsFilePath { get; }

    public PresetService()
    {
        var baseDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "NSPdfMerge");

        Directory.CreateDirectory(baseDir);
        PresetsFilePath = Path.Combine(baseDir, "presets.json");
    }

    public PresetsData Load()
    {
        try
        {
            if (!File.Exists(PresetsFilePath))
            {
                // Try to migrate from legacy settings file once.
                var settings = new AppSettingsService().Load();
                var data = MigrateFromSettings(settings);
                if (data.SearchPathPresets.Count > 0 || data.OutputPathPresets.Count > 0)
                {
                    Save(data);
                }
                return data;
            }

            var json = File.ReadAllText(PresetsFilePath);
            var loaded = JsonSerializer.Deserialize<PresetsData>(json, _jsonOptions);
            return loaded ?? new PresetsData();
        }
        catch (Exception ex)
        {
            AppLog.Error("Failed to load presets", ex);
            return new PresetsData();
        }
    }

    public void Save(PresetsData data)
    {
        try
        {
            var json = JsonSerializer.Serialize(data, _jsonOptions);
            File.WriteAllText(PresetsFilePath, json);
        }
        catch (Exception ex)
        {
            AppLog.Error("Failed to save presets", ex);
        }
    }

    private static PresetsData MigrateFromSettings(AppSettings settings)
    {
        var data = new PresetsData();

        if (settings.PathPresets is { Count: > 0 })
        {
            foreach (var legacy in settings.PathPresets)
            {
                if (!string.IsNullOrWhiteSpace(legacy.SearchPath))
                    data.SearchPathPresets.Add(new PathPreset { Name = legacy.Name, SearchPath = legacy.SearchPath });
                if (!string.IsNullOrWhiteSpace(legacy.OutputPath))
                    data.OutputPathPresets.Add(new PathPreset { Name = legacy.Name, OutputPath = legacy.OutputPath });
            }
        }

        return data;
    }
}
