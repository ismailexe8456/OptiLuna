using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NXG.Services;

namespace NXG.ViewModels;

public partial class FocusModeViewModel : ObservableObject
{
    private readonly IFocusModeService _focusService;
    private readonly ILoggingService _logger;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsFocusInactive))]
    private bool _isFocusActive;

    public bool IsFocusInactive => !IsFocusActive;

    [ObservableProperty]
    private string _timeRemainingText = "25:00";

    [ObservableProperty]
    private int _durationMinutes = 25;

    [ObservableProperty]
    private bool _muteNotifications = true;

    [ObservableProperty]
    private bool _blockDistractions = true;

    [ObservableProperty]
    private string _newBlockedAppInput = string.Empty;

    public ObservableCollection<string> BlockedApps { get; } = new();

    public FocusModeViewModel(IFocusModeService focusService, ILoggingService logger)
    {
        _focusService = focusService;
        _logger = logger;

        _focusService.TimerTick += (s, time) =>
        {
            App.DispatcherQueue.TryEnqueue(() =>
            {
                TimeRemainingText = $"{time.Minutes:D2}:{time.Seconds:D2}";
            });
        };

        _focusService.FocusEnded += (s, e) =>
        {
            App.DispatcherQueue.TryEnqueue(() =>
            {
                IsFocusActive = false;
                TimeRemainingText = $"{DurationMinutes:D2}:00";
            });
        };

        IsFocusActive = _focusService.IsFocusActive;
        LoadBlockedApps();
    }

    private void LoadBlockedApps()
    {
        BlockedApps.Clear();
        foreach (var app in _focusService.GetBlockedApps())
        {
            BlockedApps.Add(app);
        }
    }

    [RelayCommand]
    private void SelectPreset(int minutes)
    {
        if (IsFocusActive) return;
        DurationMinutes = minutes;
        TimeRemainingText = $"{DurationMinutes:D2}:00";
    }

    [RelayCommand]
    private async Task ToggleFocusAsync()
    {
        if (IsFocusActive)
        {
            await _focusService.StopFocusAsync();
            IsFocusActive = false;
            TimeRemainingText = $"{DurationMinutes:D2}:00";
        }
        else
        {
            IsFocusActive = await _focusService.StartFocusAsync(DurationMinutes, MuteNotifications, BlockDistractions);
            if (!IsFocusActive)
            {
                _logger.LogWarning("Focus Mode", "Failed to start Focus session.");
            }
        }
    }

    [RelayCommand]
    private void AddBlockedApp()
    {
        if (string.IsNullOrWhiteSpace(NewBlockedAppInput)) return;

        string app = NewBlockedAppInput.Trim().ToLower();
        var current = _focusService.GetBlockedApps();
        if (!current.Contains(app))
        {
            current.Add(app);
            _focusService.SetBlockedApps(current);
            LoadBlockedApps();
        }
        NewBlockedAppInput = string.Empty;
    }

    [RelayCommand]
    private void RemoveBlockedApp(string app)
    {
        var current = _focusService.GetBlockedApps();
        if (current.Contains(app))
        {
            current.Remove(app);
            _focusService.SetBlockedApps(current);
            LoadBlockedApps();
        }
    }
}
