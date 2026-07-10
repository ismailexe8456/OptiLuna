using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Dtrl.Services;

public class AppBoosterService : IAppBoosterService
{
    private readonly ILoggingService _logger;
    private readonly IRecoveryService _recovery;

    private readonly List<string> _predefinedGames = new()
    {
        "cs2.exe", "minecraft.exe", "valorant.exe", "gta5.exe", "cyberpunk2077.exe",
        "fortniteclient-win64-shipping.exe", "leagueoflegends.exe", "hl2.exe", "r5apex.exe", "cod.exe"
    };

    private readonly List<string> _customGames = new();
    private readonly List<int> _suspendedProcessIds = new();
    
    private Process? _boostedProcess;
    private ProcessPriorityClass _originalPriority = ProcessPriorityClass.Normal;

    public bool IsBoostActive { get; private set; }
    public string BoostedGameName { get; private set; } = string.Empty;

    // P/Invoke for process suspension
    [DllImport("ntdll.dll", EntryPoint = "NtSuspendProcess")]
    private static extern int NtSuspendProcess(IntPtr processHandle);

    [DllImport("ntdll.dll", EntryPoint = "NtResumeProcess")]
    private static extern int NtResumeProcess(IntPtr processHandle);

    public AppBoosterService(ILoggingService logger, IRecoveryService recovery)
    {
        _logger = logger;
        _recovery = recovery;
        LoadCustomGames();
    }

    private void LoadCustomGames()
    {
        string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "custom_games.txt");
        if (File.Exists(path))
        {
            try
            {
                var lines = File.ReadAllLines(path);
                _customGames.AddRange(lines.Where(l => !string.IsNullOrWhiteSpace(l)));
            }
            catch { }
        }
    }

    private void SaveCustomGames()
    {
        string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "custom_games.txt");
        try
        {
            File.WriteAllLines(path, _customGames);
        }
        catch { }
    }

    public List<string> GetDetectedGames()
    {
        var running = Process.GetProcesses();
        var detected = new List<string>();

        foreach (var p in running)
        {
            try
            {
                string name = p.ProcessName.ToLower() + ".exe";
                if (_predefinedGames.Contains(name) || _customGames.Contains(name))
                {
                    if (!detected.Contains(p.ProcessName + ".exe"))
                    {
                        detected.Add(p.ProcessName + ".exe");
                    }
                }
            }
            catch { }
        }

        return detected;
    }

    public List<string> GetCustomGames() => _customGames;

    public void AddCustomGame(string exePath)
    {
        string name = Path.GetFileName(exePath).ToLower();
        if (!string.IsNullOrEmpty(name) && !_customGames.Contains(name))
        {
            _customGames.Add(name);
            SaveCustomGames();
            _logger.Log("App Booster", $"Added custom game: {name}");
        }
    }

    public void RemoveCustomGame(string exePath)
    {
        string name = Path.GetFileName(exePath).ToLower();
        if (_customGames.Contains(name))
        {
            _customGames.Remove(name);
            SaveCustomGames();
            _logger.Log("App Booster", $"Removed custom game: {name}");
        }
    }

    public async Task<bool> StartBoostAsync(string gameExeName)
    {
        if (IsBoostActive) return false;

        string procName = Path.GetFileNameWithoutExtension(gameExeName);
        var processes = Process.GetProcessesByName(procName);
        if (processes.Length == 0)
        {
            _logger.LogWarning("App Booster Failed", $"Process '{gameExeName}' not currently running.");
            return false;
        }

        _boostedProcess = processes[0];
        BoostedGameName = gameExeName;
        IsBoostActive = true;

        _logger.Log("App Booster", $"Initiating Boost sequence for '{gameExeName}'...");

        try
        {
            // 1. Raise game priority
            _originalPriority = _boostedProcess.PriorityClass;
            _boostedProcess.PriorityClass = ProcessPriorityClass.High;
            _logger.Log("App Booster", $"Raised '{gameExeName}' priority from {_originalPriority} to High.");

            // Register exit handler
            _boostedProcess.EnableRaisingEvents = true;
            _boostedProcess.Exited += async (s, e) =>
            {
                _logger.Log("App Booster", $"Game process '{BoostedGameName}' closed. Ending boost.");
                await StopBoostAsync();
            };
        }
        catch (Exception ex)
        {
            _logger.LogError("App Booster Priority Fail", ex.Message);
        }

        // 2. Suspend non-critical background processes
        await Task.Run(() =>
        {
            var bloatProcessNames = new[] { "searchindexer", "spoolsv", "onedrive", "mobsync", "compattelrunner" };
            foreach (var bloatName in bloatProcessNames)
            {
                var bloats = Process.GetProcessesByName(bloatName);
                foreach (var b in bloats)
                {
                    try
                    {
                        NtSuspendProcess(b.Handle);
                        _suspendedProcessIds.Add(b.Id);
                        _logger.Log("App Booster Suspend", $"Suspended background bloat process: '{b.ProcessName}' (PID: {b.Id})");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning("App Booster Suspend Fail", $"Cannot suspend '{b.ProcessName}': {ex.Message}");
                    }
                }
            }
        });

        return true;
    }

    public async Task<bool> StopBoostAsync()
    {
        if (!IsBoostActive) return false;

        _logger.Log("App Booster", $"Terminating Boost sequence for '{BoostedGameName}'...");

        // 1. Restore game priority
        if (_boostedProcess != null)
        {
            try
            {
                if (!_boostedProcess.HasExited)
                {
                    _boostedProcess.PriorityClass = _originalPriority;
                    _logger.Log("App Booster", $"Restored game process priority to {_originalPriority}.");
                }
            }
            catch { }
            _boostedProcess = null;
        }

        // 2. Resume suspended processes
        await Task.Run(() =>
        {
            foreach (int pid in _suspendedProcessIds)
            {
                try
                {
                    var p = Process.GetProcessById(pid);
                    NtResumeProcess(p.Handle);
                    _logger.Log("App Booster Resume", $"Resumed background process: '{p.ProcessName}' (PID: {pid})");
                }
                catch { }
            }
            _suspendedProcessIds.Clear();
        });

        IsBoostActive = false;
        BoostedGameName = string.Empty;
        return true;
    }
}
