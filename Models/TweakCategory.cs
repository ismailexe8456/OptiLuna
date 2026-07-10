namespace Dtrl.Models;

public enum TweakCategory
{
    Telemetry,       // Disables telemetry, diagnostic data collection, ads ID, telemetry services.
    Performance,     // System responsiveness, MMCSS prioritization, kernel paging settings.
    Gaming,          // Game Mode, FSO, GPU performance priority, latency-sensitive polling.
    Network,         // TCP Autotuning, TCP Offloads, NetBIOS, custom adapters tuning.
    Services,        // Safe-to-disable Windows system services.
    Storage,         // Hibernate configurations, last access timestamps, search indexer settings.
    Visuals,         // Animations, transparency, menu fading, drop shadows.
    WindowsUpdate,   // Windows update active hours, exclusion of driver updates, delivery optimization.
    Privacy,         // Diagnostic history, location permissions, background apps.
    Policies,        // General OS group policies mapped to registry values.
    Debloat          // Debloating tasks (Widgets, OneDrive, UWP app removals).
}
