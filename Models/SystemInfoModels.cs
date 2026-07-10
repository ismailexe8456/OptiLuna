namespace Dtrl.Models;

public class AppInfo
{
    public string Name { get; set; } = string.Empty;
    public string Publisher { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string InstallDate { get; set; } = string.Empty;
}

public class DriverInfo
{
    public string DeviceName { get; set; } = string.Empty;
    public string FriendlyName { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string DriverVersion { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
}

public class ServiceGridInfo
{
    public string ServiceName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string StartupType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class RuntimeInfo
{
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public bool IsInstalled { get; set; }

    public Microsoft.UI.Xaml.Visibility InstalledVisibility => IsInstalled ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;
}

public class BootTimeEntry
{
    public string Date { get; set; } = string.Empty;
    public double BootTimeSeconds { get; set; }
}
