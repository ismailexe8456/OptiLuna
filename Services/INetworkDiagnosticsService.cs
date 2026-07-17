using System.Collections.Generic;
using System.Threading.Tasks;
using NXG.Models;

namespace NXG.Services;

public interface INetworkDiagnosticsService
{
    Task<List<DnsBenchmarkResult>> BenchmarkDnsAsync();
    Task<NetworkPingResult> RunDiagnosticTestsAsync(string targetHost = "8.8.8.8", int packetCount = 10);
    bool ResetWinsockAndTcpIp(out string message);
    Task<int> FindOptimalMtuAsync(string targetHost = "8.8.8.8");
    bool SetSystemDns(string primary, string secondary, out string message);
    bool OptimizeInternet(out string message);
}
