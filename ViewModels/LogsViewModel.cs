using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dtrl.Models;
using Dtrl.Services;

namespace Dtrl.ViewModels;

public partial class LogsViewModel : ObservableObject
{
    private readonly ILoggingService _logger;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusText = "Audit trail logs active";

    public ObservableCollection<LogEntry> AuditLogs { get; } = new();

    public LogsViewModel(ILoggingService logger)
    {
        _logger = logger;
        RefreshLogs();
    }

    [RelayCommand]
    public void RefreshLogs()
    {
        AuditLogs.Clear();
        foreach (var log in _logger.GetLogs())
        {
            AuditLogs.Add(log);
        }
    }

    [RelayCommand]
    private void ClearLogs()
    {
        _logger.ClearLogs();
        RefreshLogs();
        StatusText = "Audit log history cleared.";
    }

    [RelayCommand]
    private async Task ExportJsonAsync()
    {
        IsBusy = true;
        StatusText = "Generating JSON export data...";

        string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string dtrlDir = Path.Combine(appData, "DTRL", "Logs");
        Directory.CreateDirectory(dtrlDir);
        string filePath = Path.Combine(dtrlDir, $"Audit_History_{DateTime.Now:yyyyMMdd_HHmmss}.json");

        await Task.Run(() =>
        {
            string json = _logger.ExportToJson();
            File.WriteAllText(filePath, json);
        });

        StatusText = $"Audit trail exported successfully to LocalAppData/DTRL/Logs";
        IsBusy = false;
    }

    [RelayCommand]
    private async Task ExportCsvAsync()
    {
        IsBusy = true;
        StatusText = "Generating CSV export data...";

        string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string dtrlDir = Path.Combine(appData, "DTRL", "Logs");
        Directory.CreateDirectory(dtrlDir);
        string filePath = Path.Combine(dtrlDir, $"Audit_History_{DateTime.Now:yyyyMMdd_HHmmss}.csv");

        await Task.Run(() =>
        {
            string csv = _logger.ExportToCsv();
            File.WriteAllText(filePath, csv);
        });

        StatusText = $"Audit trail exported successfully to LocalAppData/DTRL/Logs";
        IsBusy = false;
    }
}
