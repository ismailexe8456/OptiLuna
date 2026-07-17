using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text.Json;
using NXG.Models;

namespace NXG.Services;

public class RecoveryService : IRecoveryService
{
    private readonly ILoggingService _logger;
    private readonly string _backupFilePath;
    private readonly List<RegistryBackupEntry> _backups = new();
    private readonly object _backupLock = new();
    private readonly List<Tweak> _allTweaks;

    // Structs for SRSetRestorePointW
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct RESTOREPOINTINFOW
    {
        public int dwEventType;
        public int dwRestorePointType;
        public long llSequenceNumber;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string szDescription;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct STATEMGRSTATUS
    {
        public int nStatus;
        public long llSequenceNumber;
    }

    [DllImport("srclient.dll", EntryPoint = "SRSetRestorePointW", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
    private static extern bool SRSetRestorePoint(ref RESTOREPOINTINFOW pRestorePtSpec, out STATEMGRSTATUS pStatus);

    private const int BEGIN_SYSTEM_CHANGE = 100;
    private const int END_SYSTEM_CHANGE = 101;
    private const int APPLICATION_INSTALL = 0;
    private const int APPLICATION_UNINSTALL = 1;
    private const int DEVICE_DRIVER_INSTALL = 10;
    private const int MODIFY_SETTINGS = 12;

    public RecoveryService(ILoggingService logger)
    {
        _logger = logger;

        string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string nxgDir = Path.Combine(appData, "NXG");
        Directory.CreateDirectory(nxgDir);
        _backupFilePath = Path.Combine(nxgDir, "backups.json");

        _allTweaks = Helpers.TweakRepository.GenerateTweaks();
        LoadBackupsFromFile();
    }

    public bool CreateSystemRestorePoint(string description, out string message)
    {
        _logger.Log("System Restore", $"Initiating Restore Point: {description}");
        
        try
        {
            var rpInfo = new RESTOREPOINTINFOW
            {
                dwEventType = BEGIN_SYSTEM_CHANGE,
                dwRestorePointType = MODIFY_SETTINGS,
                llSequenceNumber = 0,
                szDescription = description
            };

            STATEMGRSTATUS status;
            bool result = SRSetRestorePoint(ref rpInfo, out status);

            if (result)
            {
                message = $"Restore Point created successfully. Sequence Number: {status.llSequenceNumber}";
                _logger.Log("System Restore", message);
                return true;
            }
            else
            {
                int error = Marshal.GetLastWin32Error();
                message = $"Failed to create Restore Point. Win32 Error: {error}. Note: This usually requires administrator elevation and for System Restore to be enabled.";
                _logger.LogError("System Restore Failed", message);
                return false;
            }
        }
        catch (Exception ex)
        {
            message = $"Exception occurred: {ex.Message}";
            _logger.LogError("System Restore Exception", message);
            return false;
        }
    }

    private void LoadBackupsFromFile()
    {
        try
        {
            if (File.Exists(_backupFilePath))
            {
                string json = File.ReadAllText(_backupFilePath);
                var loaded = JsonSerializer.Deserialize<List<RegistryBackupEntry>>(json);
                if (loaded != null)
                {
                    lock (_backupLock)
                    {
                        _backups.Clear();
                        _backups.AddRange(loaded);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Load Backups", $"Could not load registry backups from file: {ex.Message}");
        }
    }

    private void SaveBackupsToFile()
    {
        try
        {
            lock (_backupLock)
            {
                string json = JsonSerializer.Serialize(_backups, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_backupFilePath, json);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Save Backups", $"Could not save registry backups to file: {ex.Message}");
        }
    }

    public bool BackupRegistryValue(string hive, string path, string valueName, object value, string type)
    {
        string tweakId = "";
        var matchingTweak = _allTweaks.FirstOrDefault(t => 
        {
            if (t.TargetType == "Registry")
            {
                return string.Equals(t.RegistryHive, hive, StringComparison.OrdinalIgnoreCase) &&
                       string.Equals(t.RegistryPath, path, StringComparison.OrdinalIgnoreCase) &&
                       string.Equals(t.RegistryValueName, valueName, StringComparison.OrdinalIgnoreCase);
            }
            else if (t.TargetType == "Service")
            {
                string serviceRegPath = $@"SYSTEM\CurrentControlSet\Services\{t.ServiceName}";
                return string.Equals("HKLM", hive, StringComparison.OrdinalIgnoreCase) &&
                       string.Equals(serviceRegPath, path, StringComparison.OrdinalIgnoreCase) &&
                       string.Equals("Start", valueName, StringComparison.OrdinalIgnoreCase);
            }
            return false;
        });

        if (matchingTweak != null)
        {
            tweakId = matchingTweak.Id;
        }
        else
        {
            tweakId = $"custom_{valueName}";
        }

        lock (_backupLock)
        {
            bool exists = _backups.Any(b => 
                string.Equals(b.TweakId, tweakId, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(b.Hive, hive, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(b.Path, path, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(b.ValueName, valueName, StringComparison.OrdinalIgnoreCase));

            if (!exists)
            {
                var entry = new RegistryBackupEntry
                {
                    TweakId = tweakId,
                    Hive = hive,
                    Path = path,
                    ValueName = valueName,
                    Value = value?.ToString(),
                    ValueType = type,
                    BackedUpAt = DateTime.Now
                };
                _backups.Add(entry);
                SaveBackupsToFile();
                _logger.Log("Backup Registry", $"Backed up {hive}\\{path}\\{valueName} with value {value} ({type}) for Tweak: {tweakId}");
            }
            else
            {
                _logger.Log("Backup Registry", $"Backup for Tweak: {tweakId} already exists. Retained original value.");
            }
        }
        return true;
    }

    public bool RestoreLastBackup(string tweakId)
    {
        RegistryBackupEntry? entry = null;
        lock (_backupLock)
        {
            entry = _backups.LastOrDefault(b => string.Equals(b.TweakId, tweakId, StringComparison.OrdinalIgnoreCase));
        }

        if (entry == null)
        {
            return false;
        }

        bool restored = RestoreRegistryValue(entry.Hive, entry.Path, entry.ValueName, entry.Value!, entry.ValueType);
        if (restored)
        {
            lock (_backupLock)
            {
                _backups.Remove(entry);
                SaveBackupsToFile();
            }
            return true;
        }
        return false;
    }

    public int GetBackupCount()
    {
        lock (_backupLock)
        {
            return _backups.Count;
        }
    }

    public void ClearAllBackups()
    {
        lock (_backupLock)
        {
            _backups.Clear();
            SaveBackupsToFile();
        }
        _logger.Log("Backups Cleared", "All registry backups were cleared by user.");
    }

    public bool RestoreRegistryValue(string hive, string path, string valueName, object value, string type)
    {
        try
        {
            Microsoft.Win32.RegistryKey? key = null;
            if (hive.Equals("HKLM", StringComparison.OrdinalIgnoreCase))
            {
                key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(path, true);
            }
            else
            {
                key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(path, true);
            }

            if (key == null)
            {
                // Create key if it was deleted
                if (hive.Equals("HKLM", StringComparison.OrdinalIgnoreCase))
                {
                    key = Microsoft.Win32.Registry.LocalMachine.CreateSubKey(path);
                }
                else
                {
                    key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(path);
                }
            }

            if (value == null)
            {
                key.DeleteValue(valueName, false);
                _logger.LogRollback("Registry Rollback", $"Deleted registry value: {hive}\\{path}\\{valueName}");
            }
            else
            {
                var regKind = GetRegistryValueKind(type);
                
                // Convert back values to proper type
                object finalValue = value;
                if (regKind == Microsoft.Win32.RegistryValueKind.DWord)
                {
                    finalValue = Convert.ToInt32(value.ToString());
                }
                else if (regKind == Microsoft.Win32.RegistryValueKind.QWord)
                {
                    finalValue = Convert.ToInt64(value.ToString());
                }

                key.SetValue(valueName, finalValue, regKind);
                _logger.LogRollback("Registry Rollback", $"Restored value: {hive}\\{path}\\{valueName} = {value}");
            }
            key.Close();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError("Registry Rollback Failed", $"Could not restore {hive}\\{path}\\{valueName}. Error: {ex.Message}");
            return false;
        }
    }

    public List<RestorePointInfo> GetSystemRestorePoints()
    {
        var list = new List<RestorePointInfo>();
        try
        {
            // Query root\default:SystemRestore
            var searcher = new ManagementObjectSearcher(@"root\default", "SELECT * FROM SystemRestore");
            foreach (ManagementObject obj in searcher.Get())
            {
                var creationTimeStr = obj["CreationTime"]?.ToString() ?? string.Empty;
                // WMI datetime is in format yyyymmddhhmmss...
                if (creationTimeStr.Length >= 14)
                {
                    string year = creationTimeStr.Substring(0, 4);
                    string month = creationTimeStr.Substring(4, 2);
                    string day = creationTimeStr.Substring(6, 2);
                    string hour = creationTimeStr.Substring(8, 2);
                    string minute = creationTimeStr.Substring(10, 2);
                    string second = creationTimeStr.Substring(12, 2);
                    creationTimeStr = $"{year}-{month}-{day} {hour}:{minute}:{second}";
                }

                list.Add(new RestorePointInfo
                {
                    SequenceNumber = (uint)obj["SequenceNumber"],
                    Description = obj["Description"]?.ToString() ?? "System Restore Point",
                    RestorePointType = (uint)obj["RestorePointType"],
                    CreatedAt = creationTimeStr
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Get Restore Points", $"Could not load restore points: {ex.Message}. (WMI access might require administrative rights)");
        }
        return list;
    }

    private Microsoft.Win32.RegistryValueKind GetRegistryValueKind(string type)
    {
        return type.ToLower() switch
        {
            "dword" => Microsoft.Win32.RegistryValueKind.DWord,
            "qword" => Microsoft.Win32.RegistryValueKind.QWord,
            "binary" => Microsoft.Win32.RegistryValueKind.Binary,
            _ => Microsoft.Win32.RegistryValueKind.String
        };
    }
}
