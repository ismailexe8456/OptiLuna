using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dtrl.Services;
using Dtrl.Models;

namespace Dtrl.ViewModels;

public partial class NetworkViewModel : ObservableObject
{
    private readonly INetworkDiagnosticsService _networkService;
    private readonly ILoggingService _logger;

    [ObservableProperty]
    private string _pingTarget = "8.8.8.8";

    [ObservableProperty]
    private double _latencyMs;

    [ObservableProperty]
    private double _jitterMs;

    [ObservableProperty]
    private double _packetLoss;

    [ObservableProperty]
    private string _diagnosticMessage = "Idle";

    [ObservableProperty]
    private string _optimalMtuText = "Optimal MTU: Unchecked";

    [ObservableProperty]
    private string _dnsStatusText = "DNS: System Default";

    [ObservableProperty]
    private bool _isBusy;

    public ObservableCollection<DnsBenchmarkResult> DnsResults { get; } = new();

    public NetworkViewModel(INetworkDiagnosticsService networkService, ILoggingService logger)
    {
        _networkService = networkService;
        _logger = logger;
    }

    [RelayCommand]
    private async Task RunDiagnosticsAsync()
    {
        IsBusy = true;
        DiagnosticMessage = "Testing ping roundtrip latency, packet loss, and jitter...";
        
        var result = await _networkService.RunDiagnosticTestsAsync(PingTarget, 10);
        
        LatencyMs = result.AverageLatencyMs;
        JitterMs = result.JitterMs;
        PacketLoss = result.PacketLossPercentage;
        DiagnosticMessage = result.DiagnosticMessage;
        
        IsBusy = false;
    }

    [RelayCommand]
    private async Task BenchmarkDnsAsync()
    {
        IsBusy = true;
        DnsResults.Clear();
        DiagnosticMessage = "Benchmarking DNS Providers... (Resolving test domains)";

        var list = await _networkService.BenchmarkDnsAsync();
        foreach (var item in list)
        {
            DnsResults.Add(item);
        }

        DiagnosticMessage = "DNS Benchmark complete.";
        IsBusy = false;
    }

    [RelayCommand]
    private async Task RunWinsockResetAsync()
    {
        IsBusy = true;
        DiagnosticMessage = "Resetting Winsock catalog & TCP/IP stack...";
        
        string msg = "";
        bool result = await Task.Run(() => _networkService.ResetWinsockAndTcpIp(out msg));
        
        DiagnosticMessage = msg;
        IsBusy = false;
    }

    [RelayCommand]
    private async Task OptimizeInternetAsync()
    {
        IsBusy = true;
        DiagnosticMessage = "Applying advanced TCP/IP tuning and optimizations...";

        string msg = "";
        bool result = await Task.Run(() => _networkService.OptimizeInternet(out msg));

        DiagnosticMessage = msg;
        IsBusy = false;
    }

    [RelayCommand]
    private async Task FlushDnsAsync()
    {
        IsBusy = true;
        DiagnosticMessage = "Flushing DNS resolver cache...";

        await Task.Run(() =>
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "ipconfig",
                    Arguments = "/flushdns",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                var p = Process.Start(psi);
                p?.WaitForExit();
                _logger.Log("Flush DNS", "Flushed DNS Resolver Cache successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError("Flush DNS Failed", ex.Message);
            }
        });

        DiagnosticMessage = "DNS Resolver Cache flushed successfully.";
        IsBusy = false;
    }

    [RelayCommand]
    private async Task FindOptimalMtuAsync()
    {
        IsBusy = true;
        DiagnosticMessage = "Scanning optimal MTU size using ping sweep...";

        int mtu = await _networkService.FindOptimalMtuAsync(PingTarget);
        OptimalMtuText = $"Optimal MTU: {mtu} bytes (Safe)";
        
        DiagnosticMessage = $"MTU sweep completed. Optimal value is {mtu} bytes.";
        IsBusy = false;
    }

    [RelayCommand]
    private async Task ChangeDnsAsync(string provider)
    {
        IsBusy = true;
        DiagnosticMessage = $"Updating primary/secondary DNS servers to {provider}...";

        string primary = "1.1.1.1";
        string secondary = "1.0.0.1";

        switch (provider.ToLower())
        {
            case "cloudflare":
                primary = "1.1.1.1";
                secondary = "1.0.0.1";
                break;
            case "google":
                primary = "8.8.8.8";
                secondary = "8.8.4.4";
                break;
            case "quad9":
                primary = "9.9.9.9";
                secondary = "149.112.112.112";
                break;
            case "opendns":
                primary = "208.67.222.222";
                secondary = "208.67.220.220";
                break;
        }

        string msg = "";
        bool result = await Task.Run(() => _networkService.SetSystemDns(primary, secondary, out msg));
        
        DnsStatusText = result ? $"DNS: {provider} ({primary})" : "DNS: Change Failed";
        DiagnosticMessage = msg;
        IsBusy = false;
    }
}
