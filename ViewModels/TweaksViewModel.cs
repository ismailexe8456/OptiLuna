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

public partial class TweaksViewModel : ObservableObject
{
    private readonly ITweakService _tweakService;
    private readonly ILoggingService _logger;
    private readonly IRecoveryService _recovery;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _selectedCategory = "All";

    [ObservableProperty]
    private string _selectedRisk = "All";

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _disableConfirmations;

    [ObservableProperty]
    private Tweak? _selectedTweak;

    public ObservableCollection<Tweak> DisplayedTweaks { get; } = new();
    public List<string> Categories { get; } = new() { "All", "Telemetry", "Performance", "Gaming", "Network", "Services", "Storage", "Visuals", "WindowsUpdate", "Privacy", "Policies", "Debloat", "Nvidia", "Latency" };
    public List<string> RiskLevels { get; } = new() { "All", "Safe", "Advanced", "Dangerous", "Experimental" };

    public string SearchPlaceholderText => $"Type to Search for Tweaks ({_tweakService.GetTweaks().Count} available)...";

    public TweaksViewModel(ITweakService tweakService, ILoggingService logger, IRecoveryService recovery)
    {
        _tweakService = tweakService;
        _logger = logger;
        _recovery = recovery;
        
        LoadTweaks();
    }

    private void LoadTweaks()
    {
        FilterTweaks();
    }

    [RelayCommand]
    public void FilterTweaks()
    {
        DisplayedTweaks.Clear();
        var allTweaks = _tweakService.GetTweaks();

        var query = allTweaks.AsEnumerable();

        if (SelectedCategory != "All")
        {
            if (Enum.TryParse<TweakCategory>(SelectedCategory, out var category))
            {
                query = query.Where(t => t.Category == category);
            }
        }

        if (SelectedRisk != "All")
        {
            if (Enum.TryParse<RiskLevel>(SelectedRisk, out var risk))
            {
                query = query.Where(t => t.Risk == risk);
            }
        }

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            query = query.Where(t => t.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) || 
                                     t.Description.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
        }

        foreach (var tweak in query)
        {
            DisplayedTweaks.Add(tweak);
        }
    }

    [RelayCommand]
    private async Task ToggleTweakAsync(Tweak tweak)
    {
        IsBusy = true;
        if (tweak.IsApplied)
        {
            // Apply tweak (since UI toggled state is true)
            await Task.Run(() => _tweakService.ApplyTweak(tweak));
        }
        else
        {
            // Revert tweak (since UI toggled state is false)
            await Task.Run(() => _tweakService.RevertTweak(tweak));
        }
        IsBusy = false;
        FilterTweaks();
    }

    [RelayCommand]
    private async Task ApplyAllSafeAsync()
    {
        IsBusy = true;
        await Task.Run(() =>
        {
            var tweaks = _tweakService.GetTweaks().Where(t => t.Risk == RiskLevel.Safe && !t.IsApplied).ToList();
            foreach (var tweak in tweaks)
            {
                _tweakService.ApplyTweak(tweak);
            }
        });
        _tweakService.RefreshTweakStatuses();
        FilterTweaks();
        IsBusy = false;
    }

    [RelayCommand]
    private async Task RevertAllAsync()
    {
        IsBusy = true;
        await Task.Run(() =>
        {
            var tweaks = _tweakService.GetTweaks().Where(t => t.IsApplied).ToList();
            foreach (var tweak in tweaks)
            {
                _tweakService.RevertTweak(tweak);
            }
        });
        _tweakService.RefreshTweakStatuses();
        FilterTweaks();
        IsBusy = false;
    }
}
