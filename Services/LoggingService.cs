using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using NXG.Models;

namespace NXG.Services;

public class LoggingService : ILoggingService
{
    private readonly List<LogEntry> _logs = new();
    private readonly string _logFilePath;
    private readonly object _lock = new();

    public LoggingService()
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string nxgDir = Path.Combine(appData, "NXG");
        Directory.CreateDirectory(nxgDir);
        _logFilePath = Path.Combine(nxgDir, "activity.log");
        
        LoadLogsFromFile();
    }

    public void Log(string action, string details = "", string level = "Info", string tweakId = "")
    {
        var entry = new LogEntry
        {
            Timestamp = DateTime.Now,
            Level = level,
            Action = action,
            TweakId = tweakId,
            Details = details
        };

        lock (_lock)
        {
            _logs.Insert(0, entry); // Keep newest first in memory
            WriteEntryToFile(entry);
        }
    }

    public void LogWarning(string action, string details = "")
    {
        Log(action, details, "Warning");
    }

    public void LogError(string action, string details = "", string tweakId = "")
    {
        Log(action, details, "Error", tweakId);
    }

    public void LogRollback(string action, string details = "", string tweakId = "")
    {
        Log(action, details, "Rollback", tweakId);
    }

    public List<LogEntry> GetLogs()
    {
        lock (_lock)
        {
            return _logs.ToList();
        }
    }

    public void ClearLogs()
    {
        lock (_lock)
        {
            _logs.Clear();
            try
            {
                if (File.Exists(_logFilePath))
                {
                    File.Delete(_logFilePath);
                }
            }
            catch { }
        }
        Log("Logs Cleared", "Audit logs were cleared by user.");
    }

    public string ExportToJson()
    {
        lock (_lock)
        {
            return JsonSerializer.Serialize(_logs, new JsonSerializerOptions { WriteIndented = true });
        }
    }

    public string ExportToCsv()
    {
        lock (_lock)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Timestamp,Level,Action,TweakId,Details");
            foreach (var log in _logs)
            {
                string safeDetails = log.Details.Replace("\"", "\"\"");
                string safeAction = log.Action.Replace("\"", "\"\"");
                sb.AppendLine($"\"{log.Timestamp:yyyy-MM-dd HH:mm:ss}\",\"{log.Level}\",\"{safeAction}\",\"{log.TweakId}\",\"{safeDetails}\"");
            }
            return sb.ToString();
        }
    }

    private void WriteEntryToFile(LogEntry entry)
    {
        try
        {
            string line = $"[{entry.Timestamp:yyyy-MM-dd HH:mm:ss}] [{entry.Level}] Action: {entry.Action} | Tweak: {entry.TweakId} | Details: {entry.Details}";
            File.AppendAllText(_logFilePath, line + Environment.NewLine);
        }
        catch { }
    }

    private void LoadLogsFromFile()
    {
        try
        {
            if (!File.Exists(_logFilePath)) return;
            var lines = File.ReadAllLines(_logFilePath);
            foreach (var line in lines)
            {
                // Simple parsing helper (only for rendering recovery log overview)
                if (string.IsNullOrWhiteSpace(line)) continue;
                var parts = line.Split(" | ");
                if (parts.Length >= 2)
                {
                    var header = parts[0];
                    string details = parts.Length > 2 ? parts[2].Replace("Details: ", "") : "";
                    string tweakId = parts.Length > 1 ? parts[1].Replace("Tweak: ", "") : "";
                    
                    // parse header: [date] [level] Action: value
                    var openBracket1 = header.IndexOf('[');
                    var closeBracket1 = header.IndexOf(']');
                    if (openBracket1 >= 0 && closeBracket1 > openBracket1)
                    {
                        var timeStr = header.Substring(openBracket1 + 1, closeBracket1 - openBracket1 - 1);
                        var levelHeader = header.Substring(closeBracket1 + 1);
                        var openBracket2 = levelHeader.IndexOf('[');
                        var closeBracket2 = levelHeader.IndexOf(']');
                        
                        string level = "Info";
                        string action = "";
                        if (openBracket2 >= 0 && closeBracket2 > openBracket2)
                        {
                            level = levelHeader.Substring(openBracket2 + 1, closeBracket2 - openBracket2 - 1);
                            action = levelHeader.Substring(closeBracket2 + 1).Replace("Action: ", "").Trim();
                        }

                        if (DateTime.TryParse(timeStr, out var time))
                        {
                            _logs.Add(new LogEntry
                            {
                                Timestamp = time,
                                Level = level,
                                Action = action,
                                TweakId = tweakId,
                                Details = details
                            });
                        }
                    }
                }
            }
            // Sort by newest first
            _logs.Sort((a, b) => b.Timestamp.CompareTo(a.Timestamp));
        }
        catch { }
    }
}
