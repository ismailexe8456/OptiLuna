using System.Collections.Generic;

namespace NXG.Services;

public interface IRecoveryService
{
    bool CreateSystemRestorePoint(string description, out string message);
    bool BackupRegistryValue(string hive, string path, string valueName, object value, string type);
    bool RestoreRegistryValue(string hive, string path, string valueName, object value, string type);
    bool RestoreLastBackup(string tweakId);
    int GetBackupCount();
    void ClearAllBackups();
    List<RestorePointInfo> GetSystemRestorePoints();
}

public class RestorePointInfo
{
    public uint SequenceNumber { get; set; }
    public string Description { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
    public uint RestorePointType { get; set; }

    public string SequenceNumberText => SequenceNumber.ToString();
}
