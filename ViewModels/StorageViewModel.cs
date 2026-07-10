using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dtrl.Models;
using Dtrl.Services;

namespace Dtrl.ViewModels;

public partial class StorageViewModel : ObservableObject
{
    private readonly IStorageToolService _storageService;
    private readonly ILoggingService _logger;

    [ObservableProperty]
    private string _scanPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusText = "Idle";

    // Cache clean sizes
    [ObservableProperty]
    private string _tempSizeText = "Calculating...";

    [ObservableProperty]
    private string _updateSizeText = "Calculating...";

    [ObservableProperty]
    private string _directXSizeText = "Calculating...";

    [ObservableProperty]
    private string _logsSizeText = "Calculating...";

    [ObservableProperty]
    private string _totalCleanableText = "Calculating...";

    [ObservableProperty]
    private DiskItem? _treemapRoot;

    public ObservableCollection<DiskItem> LargeFiles { get; } = new();
    public ObservableCollection<DuplicateGroup> DuplicateGroups { get; } = new();

    public StorageViewModel(IStorageToolService storageService, ILoggingService logger)
    {
        _storageService = storageService;
        _logger = logger;
        
        _ = LoadCacheSizes();
    }

    [RelayCommand]
    private async Task LoadCacheSizes()
    {
        IsBusy = true;
        StatusText = "Calculating cache cleaner sizes...";
        
        var sizes = await _storageService.GetCacheSizesAsync();
        
        TempSizeText = FormatSize(sizes["Temp"]);
        UpdateSizeText = FormatSize(sizes["Update"]);
        DirectXSizeText = FormatSize(sizes["DirectX"]);
        LogsSizeText = FormatSize(sizes["Logs"]);
        
        long total = sizes.Values.Sum();
        TotalCleanableText = FormatSize(total);

        StatusText = "Caches checked successfully.";
        IsBusy = false;
    }

    [RelayCommand]
    private async Task CleanCacheAsync(string cacheType)
    {
        IsBusy = true;
        StatusText = $"Cleaning {cacheType} cache files...";
        
        long cleaned = await _storageService.CleanCacheAsync(cacheType);
        
        StatusText = $"Successfully cleaned {FormatSize(cleaned)} from {cacheType}.";
        await LoadCacheSizes();
        IsBusy = false;
    }

    [RelayCommand]
    private async Task CleanAllCachesAsync()
    {
        IsBusy = true;
        StatusText = "Executing deep clean of system caches (excluding passwords/cookies)...";

        long totalCleaned = 0;
        var types = new[] { "Temp", "Update", "DirectX", "Logs", "RecycleBin" };
        foreach (var type in types)
        {
            totalCleaned += await _storageService.CleanCacheAsync(type);
        }

        StatusText = $"Deep Clean completed. Reclaimed {FormatSize(totalCleaned)} of disk space safely.";
        await LoadCacheSizes();
        IsBusy = false;
    }

    [RelayCommand]
    private async Task ScanLargeFilesAsync()
    {
        IsBusy = true;
        StatusText = "Searching for files larger than 50MB...";
        LargeFiles.Clear();

        var list = await _storageService.ScanLargeFilesAsync(ScanPath);
        foreach (var item in list)
        {
            LargeFiles.Add(item);
        }

        StatusText = $"Found {LargeFiles.Count} large files.";
        IsBusy = false;
    }

    [RelayCommand]
    private async Task ScanDuplicatesAsync()
    {
        IsBusy = true;
        StatusText = "Scanning directory for duplicate files (Hash matches)...";
        DuplicateGroups.Clear();

        var list = await _storageService.ScanDuplicatesAsync(ScanPath);
        int groupIndex = 1;
        foreach (var group in list)
        {
            var dupGroup = new DuplicateGroup
            {
                GroupName = $"Group #{groupIndex++} ({FormatSize(group[0].SizeBytes)})",
                Files = group
            };
            DuplicateGroups.Add(dupGroup);
        }

        StatusText = $"Scan finished. Found {DuplicateGroups.Count} sets of byte-identical duplicates.";
        IsBusy = false;
    }

    [RelayCommand]
    private async Task ScanTreemapAsync()
    {
        IsBusy = true;
        StatusText = "Building directory sizing treemap (Depth 3)...";
        TreemapRoot = null;

        var rootNode = await _storageService.BuildTreemapDataAsync(ScanPath, 3);
        
        // Layout coordinate computation (normalized 0..100)
        _storageService.LayoutTreemap(rootNode, 0, 0, 100, 100);
        
        TreemapRoot = rootNode;
        StatusText = "Treemap coordinates mapped. Ready to render.";
        IsBusy = false;
    }

    private string FormatSize(long bytes)
    {
        string[] suffix = { "B", "KB", "MB", "GB", "TB" };
        int i;
        double dblSByte = bytes;
        for (i = 0; i < suffix.Length && bytes >= 1024; i++, bytes /= 1024)
        {
            dblSByte = bytes / 1024.0;
        }
        return $"{dblSByte:F2} {suffix[i]}";
    }
}
