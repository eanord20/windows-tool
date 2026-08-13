using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Windows;

namespace NSPdfMerge.App.Services;

public sealed class UpdateService : IDisposable
{
    private const string Owner = "eanord20";
    private const string Repo = "windows-tool";
    private const string AssetName = "NSPdfMerge_Setup.exe";

    private readonly HttpClient _httpClient;

    public UpdateService()
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.UserAgent.Add(
            new ProductInfoHeaderValue(AppInfo.AppName, AppInfo.CurrentVersion));
    }

    public async Task<UpdateCheckResult> CheckForUpdateAsync(CancellationToken cancellationToken = default)
    {
        var currentVersion = Assembly.GetEntryAssembly()?.GetName().Version ?? new Version(0, 0, 0, 0);
        GitHubRelease release;

        try
        {
            var response = await _httpClient.GetAsync(
                $"https://api.github.com/repos/{Owner}/{Repo}/releases/latest",
                cancellationToken);
            response.EnsureSuccessStatusCode();

            release = await response.Content.ReadFromJsonAsync<GitHubRelease>(cancellationToken)
                      ?? throw new InvalidOperationException("GitHub returned empty release data.");
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return new UpdateCheckResult { HasRelease = false };
        }

        var tagVersion = ParseVersion(release.TagName);
        var asset = release.Assets?.FirstOrDefault(a => a.Name.Equals(AssetName, StringComparison.OrdinalIgnoreCase));

        if (asset is null)
        {
            throw new InvalidOperationException($"Release asset '{AssetName}' not found.");
        }

        return new UpdateCheckResult
        {
            IsUpdateAvailable = tagVersion > currentVersion,
            LatestVersion = tagVersion,
            DownloadUrl = asset.BrowserDownloadUrl,
            ReleaseNotes = release.Body ?? string.Empty
        };
    }

    public async Task DownloadAndInstallAsync(CancellationToken cancellationToken = default)
    {
        var result = await CheckForUpdateAsync(cancellationToken);
        if (!result.IsUpdateAvailable || string.IsNullOrEmpty(result.DownloadUrl))
        {
            throw new InvalidOperationException("No update available.");
        }

        var tempPath = Path.Combine(Path.GetTempPath(), AssetName);
        using (var response = await _httpClient.GetAsync(result.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
        {
            response.EnsureSuccessStatusCode();
            await using var fs = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None);
            await response.Content.CopyToAsync(fs, cancellationToken);
        }

        var currentProcessId = Environment.ProcessId;
        var batchPath = Path.Combine(Path.GetTempPath(), "NSPdfMerge_Updater.bat");
        var batchContent =
            $"@echo off{Environment.NewLine}" +
            $":wait{Environment.NewLine}" +
            $"tasklist /FI \"PID eq {currentProcessId}\" 2>NUL | find \"{currentProcessId}\" >NUL{Environment.NewLine}" +
            $"if %ERRORLEVEL% == 0 timeout /T 1 /NOBREAK >NUL & goto wait{Environment.NewLine}" +
            $"start \"\" \"{tempPath}\" /SILENT{Environment.NewLine}" +
            $"del \"{batchPath}\"{Environment.NewLine}";

        await File.WriteAllTextAsync(batchPath, batchContent, cancellationToken);

        Process.Start(new ProcessStartInfo(batchPath)
        {
            UseShellExecute = true,
            CreateNoWindow = true
        });

        System.Windows.Application.Current.Shutdown();
        Environment.Exit(0);
    }

    private static Version ParseVersion(string tag)
    {
        var clean = tag.Trim().TrimStart('v', 'V');
        return Version.TryParse(clean, out var version) ? version : new Version(0, 0, 0, 0);
    }

    public void Dispose() => _httpClient.Dispose();

    public sealed class GitHubRelease
    {
        [JsonPropertyName("tag_name")]
        public string TagName { get; set; } = string.Empty;

        [JsonPropertyName("body")]
        public string? Body { get; set; }

        [JsonPropertyName("assets")]
        public List<GitHubAsset>? Assets { get; set; }
    }

    public sealed class GitHubAsset
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("browser_download_url")]
        public string BrowserDownloadUrl { get; set; } = string.Empty;
    }
}

public sealed class UpdateCheckResult
{
    public bool HasRelease { get; init; } = true;
    public bool IsUpdateAvailable { get; init; }
    public Version? LatestVersion { get; init; }
    public string DownloadUrl { get; init; } = string.Empty;
    public string ReleaseNotes { get; init; } = string.Empty;
}
