using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;
using NXG.Models;
using NXG.Helpers;

namespace NXG.Services;

public class TweakService : ITweakService
{
    private readonly List<Tweak> _tweaks;
    private readonly ILoggingService _logger;
    private readonly IRecoveryService _recovery;

    public TweakService(ILoggingService logger, IRecoveryService recovery)
    {
        _logger = logger;
        _recovery = recovery;
        _tweaks = TweakRepository.GenerateTweaks();
        RefreshTweakStatuses();
    }

    public List<Tweak> GetTweaks()
    {
        return _tweaks;
    }

    public bool ApplyTweak(Tweak tweak)
    {
        _logger.Log("Apply Tweak", $"Applying tweak '{tweak.Name}' (ID: {tweak.Id}, Risk: {tweak.Risk})", "Info", tweak.Id);

        try
        {
            if (tweak.TargetType == "Registry")
            {
                // Read current value for rollback before writing
                object? currentValue = ReadRegistryValue(tweak.RegistryHive, tweak.RegistryPath, tweak.RegistryValueName);
                _recovery.BackupRegistryValue(tweak.RegistryHive, tweak.RegistryPath, tweak.RegistryValueName, currentValue!, tweak.RegistryType);

                // Write new value
                WriteRegistryValue(tweak.RegistryHive, tweak.RegistryPath, tweak.RegistryValueName, tweak.ActiveValue, tweak.RegistryType);
                tweak.IsApplied = true;
            }
            else if (tweak.TargetType == "Service")
            {
                // Services startup type is stored in HKLM\SYSTEM\CurrentControlSet\Services\{ServiceName}\Start
                string serviceRegPath = $@"SYSTEM\CurrentControlSet\Services\{tweak.ServiceName}";
                object? currentStart = ReadRegistryValue("HKLM", serviceRegPath, "Start");
                _recovery.BackupRegistryValue("HKLM", serviceRegPath, "Start", currentStart!, "DWord");

                WriteRegistryValue("HKLM", serviceRegPath, "Start", tweak.ActiveStartupType, "DWord");
                
                // Stop the service immediately if disabling it
                if (tweak.ActiveStartupType == 4)
                {
                    StopService(tweak.ServiceName);
                }

                tweak.IsApplied = true;
            }
            else if (tweak.TargetType == "Shell")
            {
                ExecuteShellCommand(tweak.ShellCommand);
                tweak.IsApplied = true;
            }

            _logger.Log("Tweak Applied", $"Successfully applied tweak '{tweak.Name}'", "Info", tweak.Id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError("Apply Tweak Failed", $"Could not apply tweak '{tweak.Name}'. Error: {ex.Message}", tweak.Id);
            return false;
        }
    }

    public bool RevertTweak(Tweak tweak)
    {
        _logger.Log("Revert Tweak", $"Reverting tweak '{tweak.Name}' (ID: {tweak.Id})", "Info", tweak.Id);

        try
        {
            if (tweak.TargetType == "Registry" || tweak.TargetType == "Service")
            {
                if (_recovery.RestoreLastBackup(tweak.Id))
                {
                    tweak.IsApplied = false;
                    _logger.Log("Tweak Reverted", $"Successfully reverted tweak '{tweak.Name}' via restore backup", "Rollback", tweak.Id);
                    return true;
                }

                // Fallback
                _logger.LogWarning("Revert Fallback", $"Backup entry not found for tweak '{tweak.Id}'. Using fallback value.");

                if (tweak.TargetType == "Registry")
                {
                    WriteRegistryValue(tweak.RegistryHive, tweak.RegistryPath, tweak.RegistryValueName, tweak.UndoValue, tweak.RegistryType);
                }
                else if (tweak.TargetType == "Service")
                {
                    string serviceRegPath = $@"SYSTEM\CurrentControlSet\Services\{tweak.ServiceName}";
                    WriteRegistryValue("HKLM", serviceRegPath, "Start", tweak.UndoStartupType, "DWord");
                }
                tweak.IsApplied = false;
            }
            else if (tweak.TargetType == "Shell")
            {
                if (!string.IsNullOrEmpty(tweak.ShellUndo))
                {
                    ExecuteShellCommand(tweak.ShellUndo);
                }
                tweak.IsApplied = false;
            }

            _logger.Log("Tweak Reverted", $"Successfully reverted tweak '{tweak.Name}'", "Rollback", tweak.Id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError("Revert Tweak Failed", $"Could not revert tweak '{tweak.Name}'. Error: {ex.Message}", tweak.Id);
            return false;
        }
    }

    public void RefreshTweakStatuses()
    {
        foreach (var tweak in _tweaks)
        {
            try
            {
                if (tweak.TargetType == "Registry")
                {
                    object? val = ReadRegistryValue(tweak.RegistryHive, tweak.RegistryPath, tweak.RegistryValueName);
                    if (val != null)
                    {
                        tweak.IsApplied = val.ToString()!.Equals(tweak.ActiveValue.ToString(), StringComparison.OrdinalIgnoreCase);
                    }
                    else
                    {
                        tweak.IsApplied = false;
                    }
                }
                else if (tweak.TargetType == "Service")
                {
                    string serviceRegPath = $@"SYSTEM\CurrentControlSet\Services\{tweak.ServiceName}";
                    object? val = ReadRegistryValue("HKLM", serviceRegPath, "Start");
                    if (val != null)
                    {
                        int startType = Convert.ToInt32(val);
                        tweak.IsApplied = startType == tweak.ActiveStartupType;
                    }
                    else
                    {
                        tweak.IsApplied = false;
                    }
                }
                else if (tweak.TargetType == "Shell")
                {
                    // Shell commands cannot easily verify active state, we default to false or rely on tracking logs
                    tweak.IsApplied = false; 
                }
            }
            catch
            {
                tweak.IsApplied = false;
            }
        }
    }

    private object? ReadRegistryValue(string hive, string path, string valueName)
    {
        RegistryKey? key = hive.Equals("HKLM", StringComparison.OrdinalIgnoreCase) 
            ? Registry.LocalMachine.OpenSubKey(path, false)
            : Registry.CurrentUser.OpenSubKey(path, false);

        if (key == null) return null;
        object? val = key.GetValue(valueName);
        key.Close();
        return val;
    }

    private void WriteRegistryValue(string hive, string path, string valueName, object value, string type)
    {
        RegistryKey? key = hive.Equals("HKLM", StringComparison.OrdinalIgnoreCase)
            ? Registry.LocalMachine.OpenSubKey(path, true)
            : Registry.CurrentUser.OpenSubKey(path, true);

        if (key == null)
        {
            key = hive.Equals("HKLM", StringComparison.OrdinalIgnoreCase)
                ? Registry.LocalMachine.CreateSubKey(path)
                : Registry.CurrentUser.CreateSubKey(path);
        }

        var regKind = GetRegistryValueKind(type);
        
        object finalValue = value;
        if (regKind == RegistryValueKind.DWord)
        {
            finalValue = Convert.ToInt32(value);
        }
        else if (regKind == RegistryValueKind.QWord)
        {
            finalValue = Convert.ToInt64(value);
        }

        key.SetValue(valueName, finalValue, regKind);
        key.Close();
    }

    private void StopService(string serviceName)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c sc stop {serviceName}",
                CreateNoWindow = true,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden
            };
            Process.Start(psi)?.WaitForExit();
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Stop Service Failed", $"Could not stop service {serviceName}: {ex.Message}");
        }
    }

    private void ExecuteShellCommand(string cmd)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -NonInteractive -Command \"{cmd}\"",
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true
        };
        
        using var process = Process.Start(psi);
        if (process != null)
        {
            process.WaitForExit();
            string err = process.StandardError.ReadToEnd();
            if (!string.IsNullOrEmpty(err) && process.ExitCode != 0)
            {
                throw new Exception($"Shell command error: {err}");
            }
        }
    }

    private RegistryValueKind GetRegistryValueKind(string type)
    {
        return type.ToLower() switch
        {
            "dword" => RegistryValueKind.DWord,
            "qword" => RegistryValueKind.QWord,
            "binary" => RegistryValueKind.Binary,
            _ => RegistryValueKind.String
        };
    }
}
