using System;
using System.Threading.Tasks;
using NXG.Models;

namespace NXG.Services;

public interface IHardwareMonitorService
{
    Task<HardwareMetrics> GetMetricsAsync();
    void StartMonitoring(Action<HardwareMetrics> callback, int intervalMs = 2000);
    void StopMonitoring();
}
