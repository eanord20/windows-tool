using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using NSPdfMerge.App.Services;
using NSPdfMerge.App.ViewModels;

namespace NSPdfMerge.App;

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

    public string Changelog => AppInfo.Changelog;

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
                ? $"{LocalizationService.Instance.Get("UpdateAvailable")} {LatestVersion}"
                : LocalizationService.Instance.Get("UpdateUpToDate");
        }
        catch (Exception ex)
        {
            StatusText = $"{LocalizationService.Instance.Get("UpdateErrorPrefix")} {ex.Message}";
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
            StatusText = $"{LocalizationService.Instance.Get("UpdateErrorPrefix")} {ex.Message}";
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
