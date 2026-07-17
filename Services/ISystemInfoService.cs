using System.Collections.Generic;
using System.Threading.Tasks;
using NXG.Models;

namespace NXG.Services;

public interface ISystemInfoService
{
    Task<List<AppInfo>> GetInstalledAppsAsync();
    Task<List<DriverInfo>> GetInstalledDriversAsync();
    Task<List<ServiceGridInfo>> GetRunningServicesAsync();
    Task<List<RuntimeInfo>> GetInstalledRuntimesAsync();
    Task<List<BootTimeEntry>> GetBootTimeHistoryAsync();
}
