namespace NSPdfMerge.App;

public partial class App : System.Windows.Application
{
    public App()
    {
        // This runs before OnStartup and helps diagnose hangs during app initialization.
        NSPdfMerge.App.Services.AppLog.Info("App ctor entered");
        try
        {
            InitializeComponent();
            NSPdfMerge.App.Services.AppLog.Info("App InitializeComponent completed");
        }
        catch (Exception ex)
        {
            NSPdfMerge.App.Services.AppLog.Error("App InitializeComponent failed", ex);
            throw;
        }
    }

    protected override void OnStartup(System.Windows.StartupEventArgs e)
    {
        var settings = new NSPdfMerge.App.Services.AppSettingsService().Load();
        NSPdfMerge.App.Services.LocalizationService.ApplyLanguage(settings.Language ?? "en");

        NSPdfMerge.App.Services.AppLog.Info($"Startup. Args: {string.Join(" ", e.Args)}");
        NSPdfMerge.App.Services.AppLog.Info($"Log file: {NSPdfMerge.App.Services.AppLog.LogFilePath}");

        this.DispatcherUnhandledException += (_, exArgs) =>
        {
            NSPdfMerge.App.Services.AppLog.Error("DispatcherUnhandledException", exArgs.Exception);
            exArgs.Handled = false;
        };

        AppDomain.CurrentDomain.UnhandledException += (_, exArgs) =>
        {
            NSPdfMerge.App.Services.AppLog.Error(
                "AppDomain.UnhandledException",
                exArgs.ExceptionObject as Exception);
        };

        System.Threading.Tasks.TaskScheduler.UnobservedTaskException += (_, exArgs) =>
        {
            NSPdfMerge.App.Services.AppLog.Error("TaskScheduler.UnobservedTaskException", exArgs.Exception);
        };

        try
        {
            base.OnStartup(e);
        }
        catch (Exception ex)
        {
            NSPdfMerge.App.Services.AppLog.Error("Fatal exception during OnStartup", ex);
            throw;
        }
    }
}
