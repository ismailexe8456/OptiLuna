using System;
using System.Collections.Generic;
using System.Management;
using System.Runtime.InteropServices;
using Dtrl.Models;

namespace Dtrl.Services;

public class RecoveryService : IRecoveryService
{
    private readonly ILoggingService _logger;

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

    public bool BackupRegistryValue(string hive, string path, string valueName, object value, string type)
    {
        _logger.Log("Backup Registry", $"Backed up {hive}\\{path}\\{valueName} with value {value} ({type})");
        return true; // Virtual logs write to main audit trace which has rollback options
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
