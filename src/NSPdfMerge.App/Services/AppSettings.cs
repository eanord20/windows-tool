namespace NSPdfMerge.App.Services;

public sealed class AppSettings
{
    public string? CommonSearchPath { get; set; }
    public string? OutputPdfPath { get; set; }
    public string? OutputFileName { get; set; }
    public string? OutputFilePrefix { get; set; }
    public bool IsDarkTheme { get; set; } = false;
    public string? Language { get; set; } = "en";

    // Legacy property for migration from older settings.
    public List<PathPreset>? PathPresets { get; set; }
}

public sealed class PathPreset
{
    public string? Name { get; set; }
    public string? SearchPath { get; set; }
    public string? OutputPath { get; set; }
}
