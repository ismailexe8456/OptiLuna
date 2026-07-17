using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NXG.Models;
using NXG.Services;

namespace NXG.ViewModels;

public partial class PowerPlanViewModel : ObservableObject
{
    private readonly ITweakService _tweakService;
    private readonly ILoggingService _logger;
    private readonly IRecoveryService _recovery;

    [ObservableProperty]
    private string _selectedTab = "Desktop"; // Desktop, Laptop, Custom

    [ObservableProperty]
    private string _selectedPreset = "Balanced"; // Balanced, Max Performance, Silent

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _disableConfirmations;

    public ObservableCollection<Tweak> DisplayedSettings { get; } = new();
    public List<string> Presets { get; } = new() { "Balanced", "Max Performance", "Silent" };

    private readonly string _cpuVendor;

    public PowerPlanViewModel(ITweakService tweakService, ILoggingService logger, IRecoveryService recovery)
    {
        _tweakService = tweakService;
        _logger = logger;
        _recovery = recovery;

        // Detect CPU Vendor from Windows Registry
        string cpuName = "";
        try
        {
            using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0"))
            {
                cpuName = key?.GetValue("ProcessorNameString")?.ToString()?.ToLower() ?? "";
            }
        }
        catch { }
        _cpuVendor = cpuName.Contains("amd") || cpuName.Contains("ryzen") ? "AMD" : "Intel";

        LoadSettings();
    }

    public void LoadSettings()
    {
        DisplayedSettings.Clear();
        var allTweaks = _tweakService.GetTweaks();

        // Get power settings starting with "power_" prefix
        var powerTweaks = allTweaks.Where(t => t.Id.StartsWith("power_")).ToList();

        foreach (var tweak in powerTweaks)
        {
            // Vendor filtering
            if (tweak.Id == "power_parking_intel" && _cpuVendor == "AMD") continue;
            if (tweak.Id == "power_parking_amd" && _cpuVendor == "Intel") continue;

            // Tab filtering
            if (SelectedTab == "Desktop")
            {
                // Exclude battery/laptop specific tweaks
                if (tweak.Id == "power_usb_selective_suspend" || tweak.Id == "power_usb3_power_mgmt" || tweak.Id == "power_monitor_timeout")
                {
                    continue;
                }
            }
            else if (SelectedTab == "Laptop")
            {
                // Exclude heavy/dangerous desktop specific tweaks
                if (tweak.Id == "power_disable_freq_scaling" || tweak.Id == "power_parking_amd" || tweak.Id == "power_prefer_perf_cores")
                {
                    continue;
                }
            }

            DisplayedSettings.Add(tweak);
        }
    }

    [RelayCommand]
    private async Task ToggleSettingAsync(Tweak tweak)
    {
        IsBusy = true;
        if (tweak.IsApplied)
        {
            await Task.Run(() => _tweakService.ApplyTweak(tweak));
        }
        else
        {
            await Task.Run(() => _tweakService.RevertTweak(tweak));
        }
        IsBusy = false;
        LoadSettings();
    }

    [RelayCommand]
    private async Task ApplyPresetAsync()
    {
        IsBusy = true;
        _logger.Log("Power Preset", $"Applying power preset: {SelectedPreset}");

        await Task.Run(() =>
        {
            var tweaks = _tweakService.GetTweaks().Where(t => t.Id.StartsWith("power_")).ToList();

            if (SelectedPreset == "Max Performance")
            {
                // Turn on ultimate/high performance, disable core parking, enable turbo boost, disable throttle states
                foreach (var tweak in tweaks)
                {
                    if (tweak.Id == "power_parking_intel" && _cpuVendor == "AMD") continue;
                    if (tweak.Id == "power_parking_amd" && _cpuVendor == "Intel") continue;

                    if (tweak.Id == "power_idle_performance" || tweak.Id == "power_disable_throttle" ||
                        tweak.Id == "power_hardware_pstates" || tweak.Id == "power_turbo_boost" ||
                        tweak.Id.Contains("parking") || tweak.Id == "power_prefer_perf_cores")
                    {
                        _tweakService.ApplyTweak(tweak);
                    }
                }
            }
            else if (SelectedPreset == "Balanced")
            {
                // Revert all power configurations to system defaults
                foreach (var tweak in tweaks)
                {
                    _tweakService.RevertTweak(tweak);
                }
            }
            else if (SelectedPreset == "Silent")
            {
                // Disable Turbo Boost and enable core parking to lower temps and fan noise
                foreach (var tweak in tweaks)
                {
                    if (tweak.Id == "power_turbo_boost")
                    {
                        _tweakService.RevertTweak(tweak); // Revert to disabled boost
                    }
                    else if (tweak.Id.Contains("parking"))
                    {
                        _tweakService.RevertTweak(tweak); // Revert to parked states
                    }
                }
            }
        });

        _tweakService.RefreshTweakStatuses();
        LoadSettings();
        IsBusy = false;
    }

    [RelayCommand]
    private async Task RevertAllAsync()
    {
        IsBusy = true;
        _logger.Log("Power plan undo", "Reverting all power settings on current tab to standard defaults.");

        await Task.Run(() =>
        {
            foreach (var tweak in DisplayedSettings)
            {
                if (tweak.IsApplied)
                {
                    _tweakService.RevertTweak(tweak);
                }
            }
        });

        _tweakService.RefreshTweakStatuses();
        LoadSettings();
        IsBusy = false;
    }
}
