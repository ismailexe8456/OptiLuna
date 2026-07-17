using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using NXG.Services;
using NXG.ViewModels;

namespace NXG;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// The main application window. Use <c>App.Window</c> from any class that needs
    /// the window reference (for dialogs, pickers, interop, etc.).
    /// </summary>
    public static Window Window { get; private set; } = null!;

    /// <summary>
    /// The UI thread dispatcher. Use <c>App.DispatcherQueue</c> to marshal calls
    /// to the UI thread.
    /// </summary>
    public static Microsoft.UI.Dispatching.DispatcherQueue DispatcherQueue { get; private set; } = null!;

    /// <summary>
    /// The Dependency Injection Service Provider.
    /// </summary>
    public static IServiceProvider Services { get; private set; } = null!;

    /// <summary>
    /// The native window handle (HWND).
    /// </summary>
    public static nint WindowHandle =>
        WinRT.Interop.WindowNative.GetWindowHandle(Window);

    /// <summary>
    /// Initializes the singleton application object.
    /// </summary>
    public App()
    {
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            LogCrash(e.ExceptionObject?.ToString() ?? "Unknown AppDomain Unhandled Exception");
        };

        UnhandledException += (s, e) =>
        {
            LogCrash(e.Exception?.ToString() ?? "Unknown WinUI Unhandled Exception");
            e.Handled = true;
        };

        InitializeComponent();

        var serviceCollection = new ServiceCollection();

        // Register core services
        serviceCollection.AddSingleton<ILoggingService, LoggingService>();
        serviceCollection.AddSingleton<IRecoveryService, RecoveryService>();
        serviceCollection.AddSingleton<ITweakService, TweakService>();
        serviceCollection.AddSingleton<IHardwareMonitorService, HardwareMonitorService>();
        serviceCollection.AddSingleton<IStorageToolService, StorageToolService>();
        serviceCollection.AddSingleton<INetworkDiagnosticsService, NetworkDiagnosticsService>();
        serviceCollection.AddSingleton<ISystemInfoService, SystemInfoService>();
        serviceCollection.AddSingleton<IBenchmarkService, BenchmarkService>();
        serviceCollection.AddSingleton<IProfileService, ProfileService>();
        serviceCollection.AddSingleton<IAppBoosterService, AppBoosterService>();
        serviceCollection.AddSingleton<IFocusModeService, FocusModeService>();

        // Register ViewModels
        serviceCollection.AddSingleton<MainViewModel>();
        serviceCollection.AddSingleton<DashboardViewModel>();
        serviceCollection.AddTransient<TweaksViewModel>();
        serviceCollection.AddTransient<StorageViewModel>();
        serviceCollection.AddTransient<NetworkViewModel>();
        serviceCollection.AddTransient<SystemInfoViewModel>();
        serviceCollection.AddTransient<BenchmarkViewModel>();
        serviceCollection.AddTransient<ProfilesViewModel>();
        serviceCollection.AddTransient<RecoveryViewModel>();
        serviceCollection.AddTransient<LogsViewModel>();
        serviceCollection.AddTransient<SettingsViewModel>();
        serviceCollection.AddTransient<PowerPlanViewModel>();
        serviceCollection.AddTransient<AppBoosterViewModel>();
        serviceCollection.AddTransient<FocusModeViewModel>();

        Services = serviceCollection.BuildServiceProvider();
    }

    /// <summary>
    /// Invoked when the application is launched.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        Window = new MainWindow();
        DispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        Window.Activate();

        using (var identity = System.Security.Principal.WindowsIdentity.GetCurrent())
        {
            var principal = new System.Security.Principal.WindowsPrincipal(identity);
            if (!principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator))
            {
                var loggingService = Services.GetRequiredService<ILoggingService>();
                loggingService.LogError("Admin Elevation Required", "NXG launched without Administrator privileges.");

                if (Window.Content != null)
                {
                    if (Window.Content.XamlRoot == null)
                    {
                        var tcs = new System.Threading.Tasks.TaskCompletionSource<object>();
                        if (Window.Content is FrameworkElement fe)
                        {
                            fe.Loaded += (s, e) => tcs.TrySetResult(null!);
                        }
                        else
                        {
                            tcs.SetResult(null!);
                        }
                        await tcs.Task;
                    }

                    var dialog = new Microsoft.UI.Xaml.Controls.ContentDialog
                    {
                        Title = "Administrator Rights Required",
                        Content = "NXG requires administrator rights to apply registry and service tweaks. Please restart the application as administrator.",
                        CloseButtonText = "Close Application",
                        XamlRoot = Window.Content.XamlRoot
                    };
                    await dialog.ShowAsync();
                }

                Application.Current.Exit();
                return;
            }
        }
    }

    private static void LogCrash(string content)
    {
        try
        {
            System.IO.File.WriteAllText(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crashlog.txt"), content);
        }
        catch
        {
            // Ignore failures to log
        }
    }
}

