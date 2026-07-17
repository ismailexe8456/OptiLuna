using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NXG.Services;

namespace NXG.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ILoggingService _logger;

    [ObservableProperty]
    private string _appVersion = "1.0.0 (Release build)";

    [ObservableProperty]
    private string _selectedTheme = "Dark"; // Default to dark

    public System.Collections.Generic.List<string> Themes { get; } = new() { "Dark", "Light", "System Default" };

    [ObservableProperty]
    private bool _isSafeModeEnabled = true;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _updateStatusText = "App is up to date";

    public SettingsViewModel(ILoggingService logger)
    {
        _logger = logger;
    }

    [RelayCommand]
    private async Task CheckForUpdatesAsync()
    {
        IsBusy = true;
        UpdateStatusText = "Checking update servers...";
        
        await Task.Delay(1500); // Simulate connection
        
        UpdateStatusText = "Latest version 1.0.0 is already installed.";
        _logger.Log("Update Check", "System is up-to-date.");
        IsBusy = false;
    }
}
