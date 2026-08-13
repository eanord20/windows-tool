using System.Reflection;

namespace NSPdfMerge.App.Services;

public static class AppInfo
{
    public static string AppName => Assembly.GetEntryAssembly()?.GetName().Name ?? "NSPdfMerge";

    public static string CurrentVersion
    {
        get
        {
            var version = Assembly.GetEntryAssembly()?.GetName().Version;
            return version is null ? "0.0.0.0" : version.ToString();
        }
    }

    public static string Changelog { get; } =
        "What's new in version 1.2.2:\r\n" +
        "• Built-in auto-updater: check for updates and install from GitHub Releases\r\n" +
        "• New Help → Update window with changelog and update controls\r\n\r\n" +
        "What's new in version 1.2.0:\r\n" +
        "• Unified menu style: equal height and width for drop-down lists\r\n" +
        "• Presets split into search and save, placed under the File menu\r\n" +
        "• Presets stored in a separate presets.json and edited via File → Edit presets\r\n" +
        "• Default preset name is taken from the full path\r\n" +
        "• Dark theme table text set to #F0F0F0, Duplicate badge uses dark text on yellow\r\n" +
        "• Compact top block and menu\r\n" +
        "• Highlight for NotFound rows (#F7C7AC) and duplicates (#FFF7DC)\r\n" +
        "• Excel-like text selection and cell copying\r\n" +
        "• Keep manual file selection when building PDF\r\n" +
        "• Undo last action (Ctrl+Z)\r\n" +
        "• Multi-row drag and drop\r\n" +
        "• Status bar showing the path to the merged PDF\r\n" +
        "• Open button in the top toolbar\r\n" +
        "• Remove duplicates from the end of the list\r\n" +
        "• Row number (№) column in the table\r\n" +
        "• Interface localization (RU / EN / UK)\r\n";
}
