using System.Collections.Generic;

namespace Dtrl.Models;

public class HardwareMetrics
{
    public double CpuUsage { get; set; }
    public double CpuFrequency { get; set; }
    public double CpuTemp { get; set; }
    
    public double GpuUsage { get; set; }
    public double GpuTemp { get; set; }
    public double GpuVramUsed { get; set; }
    public double GpuVramTotal { get; set; }
    
    public double RamUsed { get; set; }
    public double RamTotal { get; set; }
    public int RamSpeed { get; set; }

    public string MotherboardName { get; set; } = "Unknown";
    public string BiosVersion { get; set; } = "Unknown";
    
    public List<StorageDriveInfo> StorageDrives { get; set; } = new();
    
    public double BatteryLevel { get; set; } = 100;
    public string BatteryStatus { get; set; } = "Unknown";
    public double BatteryHealth { get; set; } = 100;
    
    public List<double> FanSpeeds { get; set; } = new();
    
    public string OsVersionName { get; set; } = "Windows 11";
    public string OsBuild { get; set; } = "Unknown";
}

public class StorageDriveInfo
{
    public string DeviceId { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string Interface { get; set; } = "SATA"; // SSD/NVMe/SATA
    public double TotalSizeGb { get; set; }
    public double FreeSpaceGb { get; set; }
    public int SmartStatus { get; set; } = 1; // 1 = OK, 0 = Failing, -1 = Unsupported
    public string SmartStatusMessage { get; set; } = "OK";
    public double Temperature { get; set; }
}
