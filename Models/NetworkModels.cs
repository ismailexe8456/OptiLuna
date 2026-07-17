namespace NXG.Models;

public class DnsBenchmarkResult
{
    public string ProviderName { get; set; } = string.Empty;
    public string PrimaryIp { get; set; } = string.Empty;
    public double AverageQueryTimeMs { get; set; }
    public string Status { get; set; } = "Unknown";
}

public class NetworkPingResult
{
    public double AverageLatencyMs { get; set; }
    public double JitterMs { get; set; }
    public double PacketLossPercentage { get; set; }
    public string DiagnosticMessage { get; set; } = string.Empty;
}
