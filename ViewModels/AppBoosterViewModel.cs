using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dtrl.Services;

namespace Dtrl.ViewModels;

public partial class AppBoosterViewModel : ObservableObject
{
    private readonly IAppBoosterService _boosterService;
    private readonly ILoggingService _logger;

    [ObservableProperty]
    private bool _isBoostActive;

    [ObservableProperty]
    private string _boostedGameName = string.Empty;

    [ObservableProperty]
    private string _customGameInput = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    public ObservableCollection<string> DetectedGames { get; } = new();
    public ObservableCollection<string> CustomGames { get; } = new();

    public AppBoosterViewModel(IAppBoosterService boosterService, ILoggingService logger)
    {
        _boosterService = boosterService;
        _logger = logger;

        RefreshState();
        LoadCustomGames();
    }

    public Microsoft.UI.Xaml.Visibility DetectedGamesEmptyVisibility => DetectedGames.Count == 0 ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;

    public void RefreshState()
    {
        IsBoostActive = _boosterService.IsBoostActive;
        BoostedGameName = _boosterService.BoostedGameName;
        
        DetectedGames.Clear();
        var detected = _boosterService.GetDetectedGames();
        foreach (var g in detected)
        {
            DetectedGames.Add(g);
        }
        OnPropertyChanged(nameof(DetectedGamesEmptyVisibility));
    }

    private void LoadCustomGames()
    {
        CustomGames.Clear();
        var customs = _boosterService.GetCustomGames();
        foreach (var c in customs)
        {
            CustomGames.Add(c);
        }
    }

    [RelayCommand]
    private void RefreshGames()
    {
        RefreshState();
    }

    [RelayCommand]
    private async Task ToggleBoostAsync(string gameName)
    {
        IsBusy = true;
        if (IsBoostActive)
        {
            await _boosterService.StopBoostAsync();
        }
        else
        {
            await _boosterService.StartBoostAsync(gameName);
        }
        RefreshState();
        IsBusy = false;
    }

    [RelayCommand]
    private void AddCustomGame()
    {
        if (string.IsNullOrWhiteSpace(CustomGameInput)) return;

        string path = CustomGameInput.Trim();
        _boosterService.AddCustomGame(path);
        
        CustomGameInput = string.Empty;
        LoadCustomGames();
        RefreshState();
    }

    [RelayCommand]
    private void RemoveCustomGame(string name)
    {
        _boosterService.RemoveCustomGame(name);
        LoadCustomGames();
        RefreshState();
    }
}
