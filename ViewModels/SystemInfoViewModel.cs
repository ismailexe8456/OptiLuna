using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NXG.Services;
using NXG.Models;

namespace NXG.ViewModels;

public partial class SystemInfoViewModel : ObservableObject
{
    private readonly ISystemInfoService _sysInfoService;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusText = "Idle";

    public ObservableCollection<AppInfo> InstalledApps { get; } = new();
    public ObservableCollection<DriverInfo> Drivers { get; } = new();
    public ObservableCollection<ServiceGridInfo> Services { get; } = new();
    public ObservableCollection<RuntimeInfo> Runtimes { get; } = new();
    public ObservableCollection<BootTimeEntry> BootLogs { get; } = new();

    public SystemInfoViewModel(ISystemInfoService sysInfoService)
    {
        _sysInfoService = sysInfoService;
        
        _ = LoadSystemData();
    }

    [RelayCommand]
    private async Task LoadSystemData()
    {
        IsBusy = true;
        StatusText = "Loading Windows system specifications, drivers, runtimes, and boot records...";

        // 1. Runtimes
        Runtimes.Clear();
        var runtimes = await _sysInfoService.GetInstalledRuntimesAsync();
        foreach (var run in runtimes) Runtimes.Add(run);

        // 2. Boot time history
        BootLogs.Clear();
        var boots = await _sysInfoService.GetBootTimeHistoryAsync();
        foreach (var boot in boots) BootLogs.Add(boot);

        // 3. Running Services
        Services.Clear();
        var svcs = await _sysInfoService.GetRunningServicesAsync();
        // Limit to first 200 for UI responsiveness
        int count = 0;
        foreach (var svc in svcs)
        {
            if (count++ > 200) break;
            Services.Add(svc);
        }

        // 4. Drivers
        Drivers.Clear();
        var drivers = await _sysInfoService.GetInstalledDriversAsync();
        count = 0;
        foreach (var drv in drivers)
        {
            if (count++ > 200) break;
            Drivers.Add(drv);
        }

        // 5. Installed Apps
        InstalledApps.Clear();
        var apps = await _sysInfoService.GetInstalledAppsAsync();
        count = 0;
        foreach (var app in apps)
        {
            if (count++ > 200) break;
            InstalledApps.Add(app);
        }

        StatusText = "System information loaded successfully.";
        IsBusy = false;
    }
}
