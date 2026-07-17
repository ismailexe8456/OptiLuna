using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NXG.Services;

namespace NXG.ViewModels;

public partial class RecoveryViewModel : ObservableObject
{
    private readonly IRecoveryService _recoveryService;
    private readonly ILoggingService _logger;

    [ObservableProperty]
    private string _newRestorePointName = "NXG Optimization Checkpoint";

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusText = "Ready to manage system recovery options";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BackupCountText))]
    private int _backupCount;

    public string BackupCountText => $"{BackupCount} backups currently active.";

    public ObservableCollection<RestorePointInfo> RestorePoints { get; } = new();

    public RecoveryViewModel(IRecoveryService recoveryService, ILoggingService logger)
    {
        _recoveryService = recoveryService;
        _logger = logger;
        
        _ = LoadRestorePoints();
        RefreshBackupCount();
    }

    [RelayCommand]
    private void ClearBackups()
    {
        _recoveryService.ClearAllBackups();
        RefreshBackupCount();
        StatusText = "All registry backups cleared.";
    }

    public void RefreshBackupCount()
    {
        BackupCount = _recoveryService.GetBackupCount();
    }

    [RelayCommand]
    private async Task LoadRestorePoints()
    {
        IsBusy = true;
        StatusText = "Loading system restore checkpoints from WMI repository...";
        RestorePoints.Clear();

        var list = await Task.Run(() => _recoveryService.GetSystemRestorePoints());
        foreach (var rp in list)
        {
            RestorePoints.Add(rp);
        }

        StatusText = $"Loaded {RestorePoints.Count} restore points.";
        IsBusy = false;
    }

    [RelayCommand]
    private async Task CreateRestorePointAsync()
    {
        if (string.IsNullOrWhiteSpace(NewRestorePointName))
        {
            StatusText = "Please enter a valid description for the restore point.";
            return;
        }

        IsBusy = true;
        StatusText = "Creating System Restore Point (This may take up to a minute)...";

        string msg = "";
        bool success = await Task.Run(() => _recoveryService.CreateSystemRestorePoint(NewRestorePointName, out msg));
        
        StatusText = msg;
        if (success)
        {
            await LoadRestorePoints();
        }
        IsBusy = false;
    }
}
