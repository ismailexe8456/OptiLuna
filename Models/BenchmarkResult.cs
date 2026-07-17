using System;

namespace NXG.Models;

public class BenchmarkResult
{
    public double CpuScore { get; set; } // Prime calculations score
    public double MemoryBandwidthMbS { get; set; } // Memory bandwidth in MB/s
    public double DiskReadMbS { get; set; } // Sequential read MB/s
    public double DiskWriteMbS { get; set; } // Sequential write MB/s
    public double BootTimeSeconds { get; set; } // System boot time from Event log
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public bool IsBeforeComparison { get; set; } = false;
}
