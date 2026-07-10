using System;
using System.Linq;
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

    public DashboardViewModel(IHardwareMonitorService hardwareMonitor, ITweakService tweakService)
    {
        _hardwareMonitor = hardwareMonitor;
        _tweakService = tweakService;

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
        
        GpuLoadText = $"{data.GpuUsage:F1}% Utilization";
        GpuVramText = $"{data.GpuVramUsed:F1} / {data.GpuVramTotal:F1} GB VRAM";
        GpuTempText = $"Temp: {data.GpuTemp:F1}°C";
        
        OsBuildText = $"Build: {data.OsBuild}";
        MotherboardText = $"MB: {data.MotherboardName}";
        BiosText = $"BIOS: {data.BiosVersion}";
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
