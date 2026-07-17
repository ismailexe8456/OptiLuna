using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Win32;
using NXG.Models;

namespace NXG.Services;

public class FocusModeService : IFocusModeService
{
    private readonly ILoggingService _logger;
    private readonly IRecoveryService _recovery;
    private readonly ITweakService _tweakService;

    private System.Timers.Timer? _countdownTimer;
    private int _secondsRemaining;
    private bool _muteNotificationsEnabled;
    private bool _closeAppsEnabled;
    
    private object? _originalNotificationSetting;
    private readonly List<string> _closedAppsList = new();
    private List<string> _blockedApps = new() { "discord", "chrome", "slack", "spotify", "teams", "steam" };

    public bool IsFocusActive { get; private set; }
    public TimeSpan TimeRemaining => TimeSpan.FromSeconds(_secondsRemaining);

    public event EventHandler<TimeSpan>? TimerTick;
    public event EventHandler? FocusEnded;

    public FocusModeService(ILoggingService logger, IRecoveryService recovery, ITweakService tweakService)
    {
        _logger = logger;
        _recovery = recovery;
        _tweakService = tweakService;
        LoadBlockedApps();
    }

    private void LoadBlockedApps()
    {
        string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "blocked_apps.txt");
        if (File.Exists(path))
        {
            try
            {
                var lines = File.ReadAllLines(path);
                _blockedApps = lines.Where(l => !string.IsNullOrWhiteSpace(l)).ToList();
            }
            catch { }
        }
    }

    private void SaveBlockedApps()
    {
        string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "blocked_apps.txt");
        try
        {
            File.WriteAllLines(path, _blockedApps);
        }
        catch { }
    }

    public List<string> GetBlockedApps() => _blockedApps;

    public void SetBlockedApps(List<string> apps)
    {
        _blockedApps = apps;
        SaveBlockedApps();
    }

    public async Task<bool> StartFocusAsync(int durationMinutes, bool muteNotifications, bool closeApps)
    {
        if (IsFocusActive) return false;

        _secondsRemaining = durationMinutes * 60;
        _muteNotificationsEnabled = muteNotifications;
        _closeAppsEnabled = closeApps;
        IsFocusActive = true;
        _closedAppsList.Clear();

        _logger.Log("Focus Mode", $"Started Focus session for {durationMinutes} minutes (Mute: {muteNotifications}, BlockApps: {closeApps})");

        // 1. Mute notifications via Registry Focus Assist override
        if (_muteNotificationsEnabled)
        {
            try
            {
                string regPath = @"Software\Microsoft\Windows\CurrentVersion\Notifications\Settings";
                using (var key = Registry.CurrentUser.OpenSubKey(regPath, false))
                {
                    _originalNotificationSetting = key?.GetValue("NOC_GLOBAL_SETTING");
                }

                // If not set, default is 1 (Show all)
                _originalNotificationSetting ??= 1;

                // Back up for safety
                _recovery.BackupRegistryValue("HKCU", regPath, "NOC_GLOBAL_SETTING", _originalNotificationSetting, "DWord");

                // Set to 3 (Alarms Only)
                using (var key = Registry.CurrentUser.OpenSubKey(regPath, true))
                {
                    key?.SetValue("NOC_GLOBAL_SETTING", 3, RegistryValueKind.DWord);
                }
                _logger.Log("Focus Mode Notifications", "Muted notifications (Focus Assist: Alarms Only).");
            }
            catch (Exception ex)
            {
                _logger.LogError("Focus Mode Notifications Fail", ex.Message);
            }
        }

        // 2. Suppress/close distraction apps
        if (_closeAppsEnabled)
        {
            await Task.Run(() =>
            {
                foreach (var appName in _blockedApps)
                {
                    var processes = Process.GetProcessesByName(appName);
                    foreach (var p in processes)
                    {
                        try
                        {
                            p.Kill();
                            if (!_closedAppsList.Contains(appName))
                            {
                                _closedAppsList.Add(appName);
                            }
                            _logger.Log("Focus Mode Distraction block", $"Closed distraction application: '{appName}'");
                        }
                        catch { }
                    }
                }
            });
        }

        // 3. Initiate Timer countdown
        _countdownTimer = new System.Timers.Timer(1000);
        _countdownTimer.Elapsed += (s, e) =>
        {
            _secondsRemaining--;
            TimerTick?.Invoke(this, TimeRemaining);

            if (_secondsRemaining <= 0)
            {
                _countdownTimer.Stop();
                _countdownTimer.Dispose();
                _countdownTimer = null;
                
                // End session
                Task.Run(async () => await EndFocusSessionAsync(true));
            }
        };
        _countdownTimer.Start();

        return true;
    }

    public async Task<bool> StopFocusAsync()
    {
        if (!IsFocusActive) return false;

        _logger.Log("Focus Mode", "Manual Focus Session Cancelled.");
        if (_countdownTimer != null)
        {
            _countdownTimer.Stop();
            _countdownTimer.Dispose();
            _countdownTimer = null;
        }

        await EndFocusSessionAsync(false);
        return true;
    }

    private async Task EndFocusSessionAsync(bool completed)
    {
        IsFocusActive = false;

        // Restore notification settings
        if (_muteNotificationsEnabled)
        {
            try
            {
                string regPath = @"Software\Microsoft\Windows\CurrentVersion\Notifications\Settings";
                using (var key = Registry.CurrentUser.OpenSubKey(regPath, true))
                {
                    if (key != null && _originalNotificationSetting != null)
                    {
                        key.SetValue("NOC_GLOBAL_SETTING", _originalNotificationSetting, RegistryValueKind.DWord);
                        _logger.Log("Focus Mode Notifications", "Restored notification state successfully.");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Focus Mode Notifications Restore Fail", ex.Message);
            }
        }

        string logMessage = completed 
            ? $"Focus Session Completed successfully! Duration met. Closed apps: {string.Join(", ", _closedAppsList)}"
            : $"Focus Session Cancelled. Restored default state. Closed apps: {string.Join(", ", _closedAppsList)}";
        
        _logger.Log("Focus Mode End", logMessage);
        
        FocusEnded?.Invoke(this, EventArgs.Empty);
    }
}
