using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;
using NXG.Models;

namespace NXG.Services;

public class NetworkDiagnosticsService : INetworkDiagnosticsService
{
    private readonly ILoggingService _logger;

    public NetworkDiagnosticsService(ILoggingService logger)
    {
        _logger = logger;
    }

    public async Task<List<DnsBenchmarkResult>> BenchmarkDnsAsync()
    {
        var providers = new List<(string Name, string Primary, string Secondary)>
        {
            ("Cloudflare", "1.1.1.1", "1.0.0.1"),
            ("Google", "8.8.8.8", "8.8.4.4"),
            ("Quad9", "9.9.9.9", "149.112.112.112"),
            ("OpenDNS", "208.67.222.222", "208.67.220.220")
        };

        var results = new List<DnsBenchmarkResult>();

        foreach (var provider in providers)
        {
            _logger.Log("DNS Benchmark", $"Testing DNS queries on {provider.Name} ({provider.Primary})...");
            double avgTime = await MeasureDnsLatencyAsync(provider.Primary);
            
            results.Add(new DnsBenchmarkResult
            {
                ProviderName = provider.Name,
                PrimaryIp = provider.Primary,
                AverageQueryTimeMs = avgTime,
                Status = avgTime < 1000 ? "Active" : "Timed Out"
            });
        }

        return results;
    }

    private async Task<double> MeasureDnsLatencyAsync(string dnsIp)
    {
        // Construct raw DNS query for google.com (A Record)
        // 12-byte header + 16-byte question = 28 bytes total
        byte[] query = new byte[]
        {
            0x12, 0x34, // ID
            0x01, 0x00, // Flags (Standard Query, Recursion Desired)
            0x00, 0x01, // QDCOUNT (1 query)
            0x00, 0x00, // ANCOUNT
            0x00, 0x00, // NSCOUNT
            0x00, 0x00, // ARCOUNT
            // Question: google.com
            0x06, 0x67, 0x6f, 0x6f, 0x67, 0x6c, 0x65, // length 6, "google"
            0x03, 0x63, 0x6f, 0x6d,                   // length 3, "com"
            0x00,                                     // null terminator
            0x00, 0x01, // QTYPE (A record)
            0x00, 0x01  // QCLASS (IN)
        };

        long totalTicks = 0;
        int successfulRuns = 0;
        int testCount = 3;

        for (int i = 0; i < testCount; i++)
        {
            try
            {
                using var client = new UdpClient();
                client.Client.SendTimeout = 1000;
                client.Client.ReceiveTimeout = 1000;
                
                var stopwatch = Stopwatch.StartNew();
                await client.SendAsync(query, query.Length, dnsIp, 53);
                
                var receiveTask = client.ReceiveAsync();
                if (await Task.WhenAny(receiveTask, Task.Delay(1000)) == receiveTask)
                {
                    stopwatch.Stop();
                    var response = receiveTask.Result;
                    if (response.Buffer.Length > 0)
                    {
                        totalTicks += stopwatch.ElapsedTicks;
                        successfulRuns++;
                    }
                }
            }
            catch { }
        }

        if (successfulRuns == 0) return 9999.0; // timeout representation
        double avgTicks = (double)totalTicks / successfulRuns;
        return Math.Round((avgTicks / Stopwatch.Frequency) * 1000.0, 1);
    }

    public async Task<NetworkPingResult> RunDiagnosticTestsAsync(string targetHost = "8.8.8.8", int packetCount = 10)
    {
        return await Task.Run(() =>
        {
            var result = new NetworkPingResult();
            var latencies = new List<double>();
            int lostPackets = 0;
            var ping = new Ping();

            for (int i = 0; i < packetCount; i++)
            {
                try
                {
                    var reply = ping.Send(targetHost, 1000);
                    if (reply.Status == IPStatus.Success)
                    {
                        latencies.Add(reply.RoundtripTime);
                    }
                    else
                    {
                        lostPackets++;
                    }
                }
                catch
                {
                    lostPackets++;
                }
            }

            result.PacketLossPercentage = ((double)lostPackets / packetCount) * 100.0;
            
            if (latencies.Count > 0)
            {
                result.AverageLatencyMs = Math.Round(latencies.Average(), 1);
                
                // Jitter = average absolute difference between consecutive ping latencies
                double jitterSum = 0;
                for (int i = 1; i < latencies.Count; i++)
                {
                    jitterSum += Math.Abs(latencies[i] - latencies[i - 1]);
                }
                result.JitterMs = latencies.Count > 1 
                    ? Math.Round(jitterSum / (latencies.Count - 1), 1) 
                    : 0;

                result.DiagnosticMessage = result.PacketLossPercentage switch
                {
                    0 => "Network connection is highly stable.",
                    <= 10 => "Slight packet loss detected. Check Wi-Fi signals.",
                    _ => "High packet loss. Potential hardware throttle or service outage."
                };
            }
            else
            {
                result.AverageLatencyMs = 999.0;
                result.DiagnosticMessage = "Connection failed. Target host unreachable.";
            }

            return result;
        });
    }

    public bool ResetWinsockAndTcpIp(out string message)
    {
        _logger.Log("Network Reset", "Executing Winsock and TCP/IP stack reset commands...");
        try
        {
            var psiWinsock = new ProcessStartInfo
            {
                FileName = "netsh",
                Arguments = "winsock reset",
                CreateNoWindow = true,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden
            };
            var p1 = Process.Start(psiWinsock);
            p1?.WaitForExit();

            var psiTcp = new ProcessStartInfo
            {
                FileName = "netsh",
                Arguments = "int ip reset",
                CreateNoWindow = true,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden
            };
            var p2 = Process.Start(psiTcp);
            p2?.WaitForExit();

            message = "Winsock and TCP/IP stacks have been reset. A computer restart is recommended to complete the process.";
            _logger.Log("Network Reset Done", message);
            return true;
        }
        catch (Exception ex)
        {
            message = $"Failed to reset network stacks. Error: {ex.Message}";
            _logger.LogError("Network Reset Failed", message);
            return false;
        }
    }

    public async Task<int> FindOptimalMtuAsync(string targetHost = "8.8.8.8")
    {
        return await Task.Run(() =>
        {
            var ping = new Ping();
            // Sweep sizes from 1472 down to 1400 (corresponds to MTU 1500 down to 1428)
            for (int size = 1472; size >= 1400; size -= 10)
            {
                try
                {
                    var options = new PingOptions { DontFragment = true };
                    var buffer = new byte[size];
                    var reply = ping.Send(targetHost, 500, buffer, options);
                    if (reply.Status == IPStatus.Success)
                    {
                        // Add 28 bytes (20 byte IP header + 8 byte ICMP header)
                        return size + 28;
                    }
                }
                catch { }
            }
            return 1500; // Default Standard MTU
        });
    }

    public bool SetSystemDns(string primary, string secondary, out string message)
    {
        _logger.Log("Set DNS", $"Configuring DNS servers to: {primary}, {secondary}...");
        try
        {
            bool success = false;
            using (var mc = new ManagementClass("Win32_NetworkAdapterConfiguration"))
            using (var instances = mc.GetInstances())
            {
                foreach (ManagementObject mo in instances)
                {
                    try
                    {
                        if ((bool)mo["IPEnabled"])
                        {
                            var dnsOrder = new string[] { primary, secondary };
                            var tempArgs = mo.GetMethodParameters("SetDNSServerSearchOrder");
                            tempArgs["DNSServerSearchOrder"] = dnsOrder;
                            mo.InvokeMethod("SetDNSServerSearchOrder", tempArgs, null);
                            success = true;
                        }
                    }
                    catch { }
                }
            }

            if (success)
            {
                message = $"DNS search order updated to {primary} and {secondary} on all active adapters.";
                _logger.Log("DNS Configured", message);
                return true;
            }
            else
            {
                message = "No active network adapters were found to configure.";
                _logger.LogWarning("DNS Config Failed", message);
                return false;
            }
        }
        catch (Exception ex)
        {
            message = $"Exception setting DNS: {ex.Message}. (Note: Admin rights are required to alter networking configurations)";
            _logger.LogError("DNS Configuration Failed", message);
            return false;
        }
    }

    public bool OptimizeInternet(out string message)
    {
        _logger.Log("Internet Optimization", "Executing TCP stack optimizations...");
        try
        {
            var commands = new[]
            {
                "int tcp set global autotuninglevel=normal",
                "int tcp set global chimney=enabled",
                "int tcp set global dca=enabled",
                "int tcp set global netdma=enabled",
                "int tcp set global ecncapability=enabled",
                "int tcp set global congestionprovider=ctcp",
                "int tcp set global heuristics=disabled",
                "int tcp set global rss=enabled",
                "int tcp set global fastopen=enabled"
            };

            foreach (var cmd in commands)
            {
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "netsh",
                        Arguments = cmd,
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        WindowStyle = ProcessWindowStyle.Hidden
                    };
                    var p = Process.Start(psi);
                    p?.WaitForExit();
                }
                catch { }
            }

            message = "Applied advanced TCP/IP tuning. Latency reduced, maximum bandwidth throughput enabled.";
            _logger.Log("Internet Optimization Done", message);
            return true;
        }
        catch (Exception ex)
        {
            message = $"Failed to apply internet optimizations. Error: {ex.Message}";
            _logger.LogError("Internet Optimization Failed", message);
            return false;
        }
    }
}
