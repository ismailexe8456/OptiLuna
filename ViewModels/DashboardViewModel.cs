using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dtrl.Models;
using Dtrl.Services;

namespace Dtrl.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly IHardwareMonitorService _hardwareMonitor;
    private readonly ITweakService _tweakService;
    private readonly IAppBoosterService _appBooster;
    private readonly IFocusModeService _focusMode;

    [ObservableProperty]
    private HardwareMetrics _metrics = new();

    [ObservableProperty]
    private int _optimizationScore = 50;

    [ObservableProperty]
    private string _optimizationScoreText = "50%";

    [ObservableProperty]
    private string _scoreStatus = "System Needs Optimization";

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    // Formatted UI bindings
    [ObservableProperty] private string _greetingHeader = "Hello";
    [ObservableProperty] private int _diskUsagePercent = 0;
    [ObservableProperty] private string _diskSpecText = "C: -- GB Free";
    [ObservableProperty] private double _reclaimableMemoryGb = 0.0;
    [ObservableProperty] private string _ramSpeedText = "Speed: -- MHz";
    [ObservableProperty] private string _boosterStatusText = "No Active Boost";

    [ObservableProperty] private string _cpuLoadText = "0% Load";
    [ObservableProperty] private string _cpuFreqText = "0.0 GHz";
    [ObservableProperty] private string _cpuTempText = "Core Temp: --°C";
    [ObservableProperty] private string _ramUsedText = "0.0 GB Used";
    [ObservableProperty] private string _ramTotalText = "0.0 GB Total";
    [ObservableProperty] private string _gpuLoadText = "0% Utilization";
    [ObservableProperty] private string _gpuVramText = "0.0 GB VRAM";
    [ObservableProperty] private string _gpuTempText = "Temp: --°C";
    [ObservableProperty] private string _osBuildText = "Build: --";
    [ObservableProperty] private string _motherboardText = "MB: --";
    [ObservableProperty] private string _biosText = "BIOS: --";

    public DashboardViewModel(IHardwareMonitorService hardwareMonitor, ITweakService tweakService, IAppBoosterService appBooster, IFocusModeService focusMode)
    {
        _hardwareMonitor = hardwareMonitor;
        _tweakService = tweakService;
        _appBooster = appBooster;
        _focusMode = focusMode;

        // Set up greeting
        int hour = DateTime.Now.Hour;
        string greeting = hour switch
        {
            < 12 => "Morning",
            < 17 => "Afternoon",
            _ => "Evening"
        };
        GreetingHeader = $"Good {greeting}, {Environment.UserName}";

        // Start listening to telemetry stream
        _hardwareMonitor.StartMonitoring(data =>
        {
            var dispatcher = App.DispatcherQueue;
            if (dispatcher == null) return;
            dispatcher.TryEnqueue(() =>
            {
                Metrics = data;
                UpdateFormattedText(data);
            });
        });

        CalculateScore();
    }

    private void UpdateFormattedText(HardwareMetrics data)
    {
        CpuLoadText = $"{data.CpuUsage:F1}% Load";
        CpuFreqText = $"{data.CpuFrequency:F2} GHz";
        CpuTempText = $"Core Temp: {data.CpuTemp:F1}°C";
        
        RamUsedText = $"{data.RamUsed:F1} GB Used";
        RamTotalText = $"{data.RamTotal:F1} GB Total";
        RamSpeedText = $"Speed: {data.RamSpeed} MHz";
        
        GpuLoadText = $"{data.GpuUsage:F1}% Utilization";
        GpuVramText = $"{data.GpuVramUsed:F1} / {data.GpuVramTotal:F1} GB VRAM";
        GpuTempText = $"Temp: {data.GpuTemp:F1}°C";
        
        OsBuildText = $"Build: {data.OsBuild}";
        MotherboardText = $"MB: {data.MotherboardName}";
        BiosText = $"BIOS: {data.BiosVersion}";

        // Disk metrics
        try
        {
            var drive = new DriveInfo("C");
            double totalSizeGB = drive.TotalSize / (1024.0 * 1024 * 1024);
            double freeSpaceGB = drive.TotalFreeSpace / (1024.0 * 1024 * 1024);
            double usedSpaceGB = totalSizeGB - freeSpaceGB;
            DiskUsagePercent = (int)((usedSpaceGB / totalSizeGB) * 100);
            DiskSpecText = $"{usedSpaceGB:F1} GB Used / {totalSizeGB:F0} GB Total";
        }
        catch
        {
            DiskUsagePercent = 50;
            DiskSpecText = "C: Status Check Blocked";
        }

        // Reclaimable cache standby memory
        try
        {
            using (var cacheCounter = new PerformanceCounter("Memory", "Cache Bytes"))
            {
                double bytes = cacheCounter.NextValue();
                ReclaimableMemoryGb = Math.Round(bytes / (1024.0 * 1024 * 1024), 2);
            }
            if (ReclaimableMemoryGb <= 0.05)
            {
                ReclaimableMemoryGb = Math.Round(data.RamUsed * 0.18, 2);
            }
        }
        catch
        {
            ReclaimableMemoryGb = Math.Round(data.RamUsed * 0.18, 2);
        }

        // App Booster Status
        BoosterStatusText = _focusMode.IsFocusActive
            ? "Focus Mode Active (Notifications Muted)"
            : _appBooster.IsBoostActive
                ? $"App Booster Active (Prioritizing {_appBooster.BoostedGameName})"
                : "No active boost detected.";
    }

    public void CalculateScore()
    {
        var tweaks = _tweakService.GetTweaks();
        var safeTweaks = tweaks.Where(t => t.Risk == RiskLevel.Safe).ToList();
        if (safeTweaks.Count == 0)
        {
            OptimizationScore = 100;
        }
        else
        {
            int appliedCount = safeTweaks.Count(t => t.IsApplied);
            OptimizationScore = (int)((double)appliedCount / safeTweaks.Count * 100);
        }

        OptimizationScoreText = $"{OptimizationScore}%";

        ScoreStatus = OptimizationScore switch
        {
            >= 90 => "System is Fully Optimized 🟢",
            >= 70 => "System is Running Great 🟡",
            _ => "Optimization Recommended 🔴"
        };
    }

    [RelayCommand]
    private async Task QuickOptimizeAsync()
    {
        IsBusy = true;
        StatusMessage = "Applying safe optimizations...";
        
        await Task.Run(() =>
        {
            var tweaks = _tweakService.GetTweaks();
            var safeTweaks = tweaks.Where(t => t.Risk == RiskLevel.Safe && !t.IsApplied).ToList();

            foreach (var tweak in safeTweaks)
            {
                _tweakService.ApplyTweak(tweak);
            }
        });

        CalculateScore();
        IsBusy = false;
        StatusMessage = "Safe optimizations applied successfully!";
    }
}
