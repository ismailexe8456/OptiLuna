using System;

namespace NXG.Models;

public class RegistryBackupEntry
{
    public string TweakId { get; set; } = string.Empty;
    public string Hive { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string ValueName { get; set; } = string.Empty;
    public string? Value { get; set; } // null = value didn't exist before (must be deleted on restore)
    public string ValueType { get; set; } = string.Empty;
    public DateTime BackedUpAt { get; set; }
}
