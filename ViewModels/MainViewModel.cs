using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Dtrl.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private object? _currentPageViewModel;

    private readonly IServiceProvider _serviceProvider;

    public MainViewModel(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        // Default to Dashboard
        NavigateTo("Dashboard");
    }

    [RelayCommand]
    public void NavigateTo(string destination)
    {
        CurrentPageViewModel = destination switch
        {
            "Dashboard" => _serviceProvider.GetService(typeof(DashboardViewModel)),
            "Tweaks" => _serviceProvider.GetService(typeof(TweaksViewModel)),
            "Network" => _serviceProvider.GetService(typeof(NetworkViewModel)),
            "Storage" => _serviceProvider.GetService(typeof(StorageViewModel)),
            "SystemInfo" => _serviceProvider.GetService(typeof(SystemInfoViewModel)),
            "Benchmarks" => _serviceProvider.GetService(typeof(BenchmarkViewModel)),
            "Profiles" => _serviceProvider.GetService(typeof(ProfilesViewModel)),
            "Recovery" => _serviceProvider.GetService(typeof(RecoveryViewModel)),
            "Logs" => _serviceProvider.GetService(typeof(LogsViewModel)),
            "Settings" => _serviceProvider.GetService(typeof(SettingsViewModel)),
            _ => CurrentPageViewModel
        };
    }
}
