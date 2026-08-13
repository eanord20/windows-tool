# Auto-Updater Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:subagent-driven-development` (recommended) or `superpowers:executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a "Check for updates" flow to the WPF app: a new Help-menu item, an update dialog, changelog display, and GitHub-release-based update check/install.

**Architecture:** Keep UI logic in the existing `MainViewModel`; introduce a single `UpdateService` for HTTP/GitHub interactions, a small `UpdateViewModel` for the dialog state, and centralized `AppInfo` constants for the changelog/version. The installer remains the Inno Setup setup produced by `installer.iss`; the updater downloads the latest `NSPdfMerge_Setup.exe` from GitHub Releases and launches it.

**Tech Stack:** WPF (.NET 8), `HttpClient` + `System.Text.Json`, existing `RelayCommand`, no new NuGet packages required.

## Global Constraints

- Target framework: `net8.0-windows`.
- No hardcoded UI dimensions/styles outside XAML/theme resources.
- All new user-visible strings must be localized in `LocalizationService` (RU, EN, UK).
- COM/CAD rules do not apply; this is a pure WPF desktop tool.
- Existing code style: file-scoped namespaces, nullable enabled, implicit usings.
- Do not commit secrets or local paths.

---

## Clarification required before implementation

> **Release distribution format:** Is the update supposed to be a GitHub Release that contains the Inno Setup installer asset named `NSPdfMerge_Setup.exe`, tagged like `v1.2.0`? If you prefer a different asset name or a portable ZIP, the plan needs adjustment.

**Feasibility:** Yes, fully realizable. Caveats:

1. The GitHub repository must have published Releases with version tags and the setup asset attached.
2. The installer requires admin rights (`PrivilegesRequired=admin`), so updating will trigger a UAC prompt.
3. Replacing the running executable requires the installer to run after the current process exits. The plan includes a tiny batch-file helper to avoid a race condition.
4. Unauthenticated GitHub API requests are limited to 60/hour, which is acceptable for a manual "Check for updates" action.

---

## File map

| File | Responsibility |
|------|----------------|
| `src/NSPdfMerge.App/AboutWindow.xaml` | Existing About dialog; remove changelog text, center on screen. |
| `src/NSPdfMerge.App/InstructionWindow.xaml` | Existing instruction dialog; center on screen. |
| `src/NSPdfMerge.App/UpdateWindow.xaml` + `.cs` | New 650×750 update dialog: current version, changelog, status, check/install buttons. |
| `src/NSPdfMerge.App/UpdateViewModel.cs` | Dialog state and commands for `UpdateWindow`. |
| `src/NSPdfMerge.App/Services/AppInfo.cs` | Centralized app metadata: current version string, changelog markdown. |
| `src/NSPdfMerge.App/Services/UpdateService.cs` | Queries GitHub Releases, downloads the setup asset, launches the installer safely. |
| `src/NSPdfMerge.App/Services/LocalizationService.cs` | Adds new localized strings for the update UI. |
| `src/NSPdfMerge.App/MainWindow.xaml` | Adds "Update" menu item under Help. |
| `src/NSPdfMerge.App/ViewModels/MainViewModel.cs` | Adds `UpdateCommand`, opens `UpdateWindow`, wires changelog. |
| `installer.iss` | Optionally switch `[Files]` source to `bin/Release/...` for release builds. |

---

## Task 1: Center About and Instruction windows on screen

**Files:**
- Modify: `src/NSPdfMerge.App/AboutWindow.xaml`
- Modify: `src/NSPdfMerge.App/InstructionWindow.xaml`
- Modify: `src/NSPdfMerge.App/ViewModels/MainViewModel.cs:282-314`

**Interfaces:**
- Consumes: existing `AboutWindow(string)` and `InstructionWindow(string)` constructors.
- Produces: dialogs open centered on the screen and are owned by the main window for modality.

- [ ] **Step 1: Change startup location in About XAML**

```xml
<Window x:Class="NSPdfMerge.App.AboutWindow"
        ...
        Title="About" Width="480" Height="420"
        WindowStartupLocation="CenterScreen"
        ResizeMode="CanResize">
```

- [ ] **Step 2: Change startup location in Instruction XAML**

```xml
<Window x:Class="NSPdfMerge.App.InstructionWindow"
        ...
        Title="Инструкция" Width="720" Height="420"
        WindowStartupLocation="CenterScreen">
```

- [ ] **Step 3: Set owner before showing dialogs**

In `MainViewModel.ShowAbout()` and `ShowInstruction()`, after creating the window:

```csharp
wnd.Owner = Application.Current.MainWindow;
wnd.ShowDialog();
```

- [ ] **Step 4: Build and visually verify**

Run: `dotnet build src/NSPdfMerge.App/NSPdfMerge.App.csproj`
Expected: builds without errors; About and Instruction open centered on screen.

- [ ] **Step 5: Commit**

```bash
git add src/NSPdfMerge.App/AboutWindow.xaml src/NSPdfMerge.App/InstructionWindow.xaml src/NSPdfMerge.App/ViewModels/MainViewModel.cs
git commit -m "ui: center About and Instruction windows on screen"
```

---

## Task 2: Extract changelog into a centralized `AppInfo` service

**Files:**
- Create: `src/NSPdfMerge.App/Services/AppInfo.cs`
- Modify: `src/NSPdfMerge.App/ViewModels/MainViewModel.cs:282-314`

**Interfaces:**
- Consumes: `Assembly.GetEntryAssembly()` version.
- Produces: `AppInfo.CurrentVersion`, `AppInfo.Changelog`.

- [ ] **Step 1: Create `AppInfo.cs`**

```csharp
using System.Reflection;

namespace NSPdfMerge.App.Services;

public static class AppInfo
{
    public static string CurrentVersion
    {
        get
        {
            var version = Assembly.GetEntryAssembly()?.GetName().Version;
            return version is null ? "0.0.0.0" : version.ToString();
        }
    }

    public static string Changelog { get; } =
        "Что нового в версии 1.2.0:\r\n" +
        "• Меню единого стиля: равная высота и ширина выпадающих списков\r\n" +
        "• Пресеты разделены на поиск и сохранение, размещены в меню Файл\r\n" +
        "• Пресеты хранятся в отдельном presets.json и редактируются через Файл → Редактировать пресеты\r\n" +
        "• Имя пресета по умолчанию берётся из полного пути\r\n" +
        "• Текст таблицы в тёмной теме вынесен в #F0F0F0, бейдж Дубль — тёмный на жёлтом\r\n" +
        "• Компактный верхний блок и меню\r\n" +
        "• Подсветка строк NotFound (#F7C7AC) и дубликатов (#FFF7DC)\r\n" +
        "• Excel-подобное выделение текста и копирование ячеек\r\n" +
        "• Сохранение ручного выбора файла при сборке PDF\r\n" +
        "• Отмена последнего действия (Ctrl+Z)\r\n" +
        "• Множественное перетаскивание строк\r\n" +
        "• Строка состояния с путём к собранному PDF\r\n" +
        "• Кнопка «Открыть» в верхней панели\r\n" +
        "• Удаление дублей с конца списка\r\n" +
        "• Порядковый номер строки (№) в таблице\r\n" +
        "• Локализация интерфейса (RU / EN / UK)\r\n";
}
```

- [ ] **Step 2: Replace inline changelog in `MainViewModel.ShowAbout`**

Remove the local `changelog` variable and use `AppInfo.Changelog` only inside the update dialog later. In `ShowAbout`, build the text as:

```csharp
private void ShowAbout()
{
    var year = DateTime.Now.Year;
    var text = $"{AppName} v{AppVersion}\r\n\r\n" +
               $"Author: {AuthorName}\r\n" +
               $"Contact:\r\n{ContactEmail}\r\n\r\n" +
               $"{AppName} is a lightweight tool designed to merge multiple PDF files into a single document.\r\n" +
               $"It is optimized for handling documentation, and large-format PDFs while preserving original quality and structure.\r\n\r\n" +
               $"© {year} {AuthorName}. All rights reserved. The program is free.";

    var wnd = new AboutWindow(text);
    wnd.Owner = Application.Current.MainWindow;
    wnd.ShowDialog();
}
```

- [ ] **Step 3: Build**

Run: `dotnet build src/NSPdfMerge.App/NSPdfMerge.App.csproj`
Expected: builds; About no longer contains the changelog.

- [ ] **Step 4: Commit**

```bash
git add src/NSPdfMerge.App/Services/AppInfo.cs src/NSPdfMerge.App/ViewModels/MainViewModel.cs
git commit -m "refactor: move changelog text to AppInfo"
```

---

## Task 3: Create the update dialog (`UpdateWindow` + `UpdateViewModel`)

**Files:**
- Create: `src/NSPdfMerge.App/UpdateWindow.xaml`
- Create: `src/NSPdfMerge.App/UpdateWindow.xaml.cs`
- Create: `src/NSPdfMerge.App/UpdateViewModel.cs`

**Interfaces:**
- Consumes: `AppInfo.CurrentVersion`, `AppInfo.Changelog`, `UpdateService.CheckForUpdateAsync`, `UpdateService.DownloadAndInstallAsync`.
- Produces: `UpdateWindow` instance shown from `MainViewModel.UpdateCommand`.

- [ ] **Step 1: Create `UpdateViewModel.cs`**

```csharp
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace NSPdfMerge.App.ViewModels;

public sealed class UpdateViewModel : INotifyPropertyChanged
{
    private readonly UpdateService _updateService;
    private string _currentVersion = AppInfo.CurrentVersion;
    private string _latestVersion = "—";
    private string _statusText = string.Empty;
    private bool _isUpdateAvailable;
    private bool _isBusy;

    public UpdateViewModel()
    {
        _updateService = new UpdateService();
        CheckCommand = new RelayCommand(async _ => await CheckAsync(), _ => !IsBusy);
        UpdateCommand = new RelayCommand(async _ => await UpdateAsync(), _ => IsUpdateAvailable && !IsBusy);
    }

    public string CurrentVersion
    {
        get => _currentVersion;
        private set => SetProperty(ref _currentVersion, value);
    }

    public string LatestVersion
    {
        get => _latestVersion;
        private set => SetProperty(ref _latestVersion, value);
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    public bool IsUpdateAvailable
    {
        get => _isUpdateAvailable;
        private set
        {
            if (SetProperty(ref _isUpdateAvailable, value))
            {
                ((RelayCommand)UpdateCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                ((RelayCommand)CheckCommand).RaiseCanExecuteChanged();
                ((RelayCommand)UpdateCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public ICommand CheckCommand { get; }
    public ICommand UpdateCommand { get; }

    private async Task CheckAsync()
    {
        IsBusy = true;
        try
        {
            var result = await _updateService.CheckForUpdateAsync();
            LatestVersion = result.LatestVersion?.ToString() ?? "—";
            IsUpdateAvailable = result.IsUpdateAvailable;
            StatusText = result.IsUpdateAvailable
                ? $"Доступна новая версия: {LatestVersion}"
                : "У вас установлена актуальная версия.";
        }
        catch (Exception ex)
        {
            StatusText = $"Ошибка проверки обновлений: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task UpdateAsync()
    {
        IsBusy = true;
        try
        {
            await _updateService.DownloadAndInstallAsync();
        }
        catch (Exception ex)
        {
            StatusText = $"Ошибка обновления: {ex.Message}";
            IsBusy = false;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}
```

- [ ] **Step 2: Add `RaiseCanExecuteChanged` to `RelayCommand`**

In `src/NSPdfMerge.App/ViewModels/RelayCommand.cs`, ensure the implementation exposes:

```csharp
public void RaiseCanExecuteChanged() => CommandManager.InvalidateRequerySuggested();
```

If the current implementation already does this, verify only.

- [ ] **Step 3: Create `UpdateWindow.xaml`**

```xml
<Window x:Class="NSPdfMerge.App.UpdateWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:loc="clr-namespace:NSPdfMerge.App.Services"
        Title="{Binding Source={x:Static loc:LocalizationService.Instance}, Path=UpdateWindowTitle}"
        Width="650" Height="750"
        WindowStartupLocation="CenterScreen"
        ResizeMode="CanResize"
        MinWidth="500" MinHeight="550">
    <Grid Margin="16">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />
            <RowDefinition Height="Auto" />
            <RowDefinition Height="Auto" />
            <RowDefinition Height="Auto" />
            <RowDefinition Height="12" />
            <RowDefinition Height="*" />
            <RowDefinition Height="12" />
            <RowDefinition Height="Auto" />
            <RowDefinition Height="12" />
            <RowDefinition Height="Auto" />
        </Grid.RowDefinitions>

        <TextBlock Grid.Row="0" FontSize="18" FontWeight="Bold"
                   Text="{Binding Source={x:Static loc:LocalizationService.Instance}, Path=UpdateWindowTitle}" />

        <TextBlock Grid.Row="1" Margin="0,8,0,0" FontSize="14">
            <Run Text="{Binding Source={x:Static loc:LocalizationService.Instance}, Path=UpdateCurrentVersion}" />
            <Run Text="{Binding CurrentVersion}" />
        </TextBlock>

        <TextBlock Grid.Row="2" Margin="0,4,0,0" FontSize="14">
            <Run Text="{Binding Source={x:Static loc:LocalizationService.Instance}, Path=UpdateLatestVersion}" />
            <Run Text="{Binding LatestVersion}" />
        </TextBlock>

        <TextBlock Grid.Row="3" Margin="0,8,0,0" FontSize="14"
                   Text="{Binding StatusText}"
                   TextWrapping="Wrap" />

        <GroupBox Grid.Row="5" Header="{Binding Source={x:Static loc:LocalizationService.Instance}, Path=UpdateChangelogHeader}">
            <ScrollViewer VerticalScrollBarVisibility="Auto">
                <TextBlock Text="{Binding Changelog}"
                           TextWrapping="Wrap"
                           FontSize="14" />
            </ScrollViewer>
        </GroupBox>

        <ProgressBar Grid.Row="7" IsIndeterminate="{Binding IsBusy}" Height="6"
                     Visibility="{Binding IsBusy, Converter={StaticResource BooleanToVisibilityConverter}}" />

        <StackPanel Grid.Row="9" Orientation="Horizontal" HorizontalAlignment="Right">
            <Button Width="150" Margin="0,0,8,0"
                    Content="{Binding Source={x:Static loc:LocalizationService.Instance}, Path=ButtonCheckUpdate}"
                    Command="{Binding CheckCommand}" />
            <Button Width="150" Margin="0,0,8,0"
                    Content="{Binding Source={x:Static loc:LocalizationService.Instance}, Path=ButtonUpdate}"
                    Command="{Binding UpdateCommand}" />
            <Button Width="110"
                    Content="{Binding Source={x:Static loc:LocalizationService.Instance}, Path=ButtonOk}"
                    Click="Ok_Click" />
        </StackPanel>
    </Grid>
</Window>
```

> Note: if `BooleanToVisibilityConverter` is not in global resources, add it to `UpdateWindow.Resources` instead:
> `<BooleanToVisibilityConverter x:Key="BooleanToVisibilityConverter" />`.

- [ ] **Step 4: Create `UpdateWindow.xaml.cs`**

```csharp
using System.Windows;
using NSPdfMerge.App.ViewModels;

namespace NSPdfMerge.App;

public partial class UpdateWindow : Window
{
    public UpdateWindow()
    {
        InitializeComponent();
        DataContext = new UpdateViewModel();
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
```

- [ ] **Step 5: Build**

Run: `dotnet build src/NSPdfMerge.App/NSPdfMerge.App.csproj`
Expected: compiles; `UpdateWindow` resolves.

- [ ] **Step 6: Commit**

```bash
git add src/NSPdfMerge.App/UpdateWindow.xaml src/NSPdfMerge.App/UpdateWindow.xaml.cs src/NSPdfMerge.App/UpdateViewModel.cs src/NSPdfMerge.App/ViewModels/RelayCommand.cs
git commit -m "feat: add update dialog and view model"
```

---

## Task 4: Implement `UpdateService` (GitHub Releases check + download/install)

**Files:**
- Create: `src/NSPdfMerge.App/Services/UpdateService.cs`

**Interfaces:**
- Consumes: `AppInfo.CurrentVersion`, GitHub Release JSON, `HttpClient`.
- Produces: `UpdateCheckResult`, `DownloadAndInstallAsync`.

- [ ] **Step 1: Create `UpdateService.cs`**

```csharp
using System.Diagnostics;
using System.IO;
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
        var response = await _httpClient.GetAsync(
            $"https://api.github.com/repos/{Owner}/{Repo}/releases/latest",
            cancellationToken);
        response.EnsureSuccessStatusCode();

        var release = await response.Content.ReadFromJsonAsync<GitHubRelease>(cancellationToken)
                      ?? throw new InvalidOperationException("GitHub returned empty release data.");

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

        // Launch installer and exit current process. Use a small batch wrapper so
        // the installer can start only after this process has exited and released the EXE.
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

        Application.Current.Shutdown();
        Environment.Exit(0);
    }

    private static Version ParseVersion(string tag)
    {
        var clean = tag.Trim().TrimStart('v', 'V');
        return Version.TryParse(clean, out var version) ? version : new Version(0, 0, 0, 0);
    }

    public void Dispose() => _httpClient.Dispose();

    private sealed class GitHubRelease
    {
        [JsonPropertyName("tag_name")]
        public string TagName { get; set; } = string.Empty;

        [JsonPropertyName("body")]
        public string? Body { get; set; }

        [JsonPropertyName("assets")]
        public List<GitHubAsset>? Assets { get; set; }
    }

    private sealed class GitHubAsset
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("browser_download_url")]
        public string BrowserDownloadUrl { get; set; } = string.Empty;
    }
}

public sealed class UpdateCheckResult
{
    public bool IsUpdateAvailable { get; init; }
    public Version? LatestVersion { get; init; }
    public string DownloadUrl { get; init; } = string.Empty;
    public string ReleaseNotes { get; init; } = string.Empty;
}
```

> Note: `AppInfo.AppName` must be added to `AppInfo.cs`:
> `public static string AppName => Assembly.GetEntryAssembly()?.GetName().Name ?? "NSPdfMerge";`

- [ ] **Step 2: Build**

Run: `dotnet build src/NSPdfMerge.App/NSPdfMerge.App.csproj`
Expected: compiles; no nullable warnings.

- [ ] **Step 3: Commit**

```bash
git add src/NSPdfMerge.App/Services/UpdateService.cs src/NSPdfMerge.App/Services/AppInfo.cs
git commit -m "feat: add GitHub release update service"
```

---

## Task 5: Add Help-menu item and wire `MainViewModel`

**Files:**
- Modify: `src/NSPdfMerge.App/MainWindow.xaml`
- Modify: `src/NSPdfMerge.App/ViewModels/MainViewModel.cs`

**Interfaces:**
- Consumes: `UpdateWindow`, `LocalizationService.MenuUpdate`.
- Produces: `MainViewModel.UpdateCommand`.

- [ ] **Step 1: Add the menu item**

In `MainWindow.xaml`, inside the `MenuHelp` `MenuItem`, insert before the first separator:

```xml
<MenuItem Header="{Binding Source={x:Static loc:LocalizationService.Instance}, Path=MenuUpdate}" Command="{Binding UpdateCommand}" />
```

- [ ] **Step 2: Add localized strings**

Add to `LocalizationService` for each language:

```csharp
["MenuUpdate"] = "Обновление",
["UpdateWindowTitle"] = "Обновление",
["UpdateCurrentVersion"] = "Текущая версия:",
["UpdateLatestVersion"] = "Последняя версия:",
["UpdateChangelogHeader"] = "Что нового",
["ButtonCheckUpdate"] = "Проверить обновление",
["ButtonUpdate"] = "Обновить",
["UpdateUpToDate"] = "У вас установлена актуальная версия.",
["UpdateAvailable"] = "Доступна новая версия:",
["UpdateErrorPrefix"] = "Ошибка:",
```

(Repeat with English and Ukrainian equivalents.)

- [ ] **Step 3: Add command and handler in `MainViewModel`**

```csharp
public ICommand UpdateCommand { get; }

// In constructor:
UpdateCommand = new RelayCommand(_ => ShowUpdate());

private void ShowUpdate()
{
    var wnd = new UpdateWindow();
    wnd.Owner = Application.Current.MainWindow;
    wnd.ShowDialog();
}
```

- [ ] **Step 4: Build and test**

Run: `dotnet build src/NSPdfMerge.App/NSPdfMerge.App.csproj`
Expected: builds; Help menu contains "Обновление"; clicking opens 650×750 window centered; changelog visible; buttons present.

- [ ] **Step 5: Commit**

```bash
git add src/NSPdfMerge.App/MainWindow.xaml src/NSPdfMerge.App/Services/LocalizationService.cs src/NSPdfMerge.App/ViewModels/MainViewModel.cs
git commit -m "feat: wire update menu and command"
```

---

## Task 6: Adjust release build source in `installer.iss`

**Files:**
- Modify: `installer.iss`

**Interfaces:**
- Consumes: `dotnet publish` Release output.
- Produces: installer used by updater.

- [ ] **Step 1: Switch installer source to Release**

Change line 16 from:
```iss
Source: "src\NSPdfMerge.App\bin\Debug\net8.0-windows\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
```
to:
```iss
Source: "src\NSPdfMerge.App\bin\Release\net8.0-windows\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
```

- [ ] **Step 2: Add release build instructions to plan / README**

Document that before creating a GitHub Release, run:
```bash
dotnet build -c Release src/NSPdfMerge.App/NSPdfMerge.App.csproj
```
Then build installer with Inno Setup and attach `NSPdfMerge_Setup.exe` to the release.

- [ ] **Step 3: Commit**

```bash
git add installer.iss
git commit -m "build: use Release output in Inno Setup installer"
```

---

## Task 7: Final verification

**Files:**
- All modified files.

- [ ] **Step 1: Full Release build**

```bash
dotnet build -c Release src/NSPdfMerge.App/NSPdfMerge.App.csproj
```
Expected: zero errors, zero warnings.

- [ ] **Step 2: Manual smoke test**

1. Run `src/NSPdfMerge.App/bin/Release/net8.0-windows/NSPdfMerge.exe`.
2. Open Help → About: verify no changelog, centered.
3. Open Help → Инструкция: verify centered.
4. Open Help → Обновление: verify 650×750, centered, changelog shown, current version shown.
5. Click "Проверить обновление" (with no release yet): should show error or "No release found" depending on API response.

- [ ] **Step 3: Create a test GitHub Release (user action)**

When ready, create a release tagged `v1.2.1` and upload `NSPdfMerge_Setup.exe`. Then re-test the check/update flow.

- [ ] **Step 4: Final commit / push (when user approves)**

```bash
git push origin master
```

---

## Self-review checklist

- [ ] Spec coverage: every user requirement (menu item, window size/centering, changelog move, check/install buttons, GitHub update logic) maps to a task.
- [ ] Placeholder scan: no "TODO", "TBD", or vague steps remain.
- [ ] Type consistency: `UpdateCheckResult.LatestVersion` is `Version?`; `UpdateService.CheckForUpdateAsync` returns `Task<UpdateCheckResult>` everywhere.
- [ ] Localization: all new strings added to RU, EN, UK dictionaries.
- [ ] DRY: changelog lives only in `AppInfo`; GitHub owner/repo/asset name live only in `UpdateService` constants.
