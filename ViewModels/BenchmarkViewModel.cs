using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NXG.Models;
using NXG.Services;

namespace NXG.ViewModels;

public partial class BenchmarkViewModel : ObservableObject
{
    private readonly IBenchmarkService _benchService;
    private readonly ILoggingService _logger;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ResultsVisibility))]
    private BenchmarkResult? _currentResult;

    [ObservableProperty]
    private BenchmarkResult? _beforeResult;

    [ObservableProperty]
    private BenchmarkResult? _afterResult;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ProgressVisibility))]
    private bool _isBusy;

    public Microsoft.UI.Xaml.Visibility ProgressVisibility => IsBusy ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;
    public Microsoft.UI.Xaml.Visibility ResultsVisibility => CurrentResult != null ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;

    [ObservableProperty]
    private string _statusText = "Ready to test performance";

    [ObservableProperty]
    private double _cpuProgressValue;

    [ObservableProperty]
    private double _memoryProgressValue;

    [ObservableProperty]
    private double _diskProgressValue;

    public ObservableCollection<BenchmarkResult> History { get; } = new();

    public BenchmarkViewModel(IBenchmarkService benchService, ILoggingService logger)
    {
        _benchService = benchService;
        _logger = logger;
    }

    [RelayCommand]
    private async Task RunBenchmarkSuiteAsync()
    {
        IsBusy = true;
        StatusText = "Beginning NXG Benchmark Suite (Do not close app)...";
        CpuProgressValue = 10;
        MemoryProgressValue = 0;
        DiskProgressValue = 0;

        string tempPath = Path.GetTempPath();

        try
        {
            StatusText = "Executing multi-threaded CPU arithmetic benchmark...";
            CpuProgressValue = 50;
            
            // Execute suite
            var result = await _benchService.RunBenchmarkSuiteAsync(tempPath);
            
            CpuProgressValue = 100;
            MemoryProgressValue = 100;
            DiskProgressValue = 100;

            CurrentResult = result;
            History.Insert(0, result);
            StatusText = "Benchmark suite completed successfully.";
        }
        catch (Exception ex)
        {
            StatusText = $"Benchmark aborted: {ex.Message}";
            _logger.LogError("Benchmark Run Failed", ex.Message);
        }

        IsBusy = false;
    }

    [RelayCommand]
    private void SetAsBeforeBaseline()
    {
        if (CurrentResult == null)
        {
            StatusText = "Please run a benchmark test first.";
            return;
        }
        BeforeResult = CurrentResult;
        BeforeResult.IsBeforeComparison = true;
        StatusText = "Baseline (Before) result set successfully.";
    }

    [RelayCommand]
    private void SetAsAfterOptimized()
    {
        if (CurrentResult == null)
        {
            StatusText = "Please run a benchmark test first.";
            return;
        }
        AfterResult = CurrentResult;
        StatusText = "Optimized (After) result set successfully.";
    }
}
