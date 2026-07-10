using System;
using System.Threading.Tasks;
using Dtrl.Models;

namespace Dtrl.Services;

public interface IHardwareMonitorService
{
    Task<HardwareMetrics> GetMetricsAsync();
    void StartMonitoring(Action<HardwareMetrics> callback, int intervalMs = 2000);
    void StopMonitoring();
}
