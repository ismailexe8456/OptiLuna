using System;
using System.Collections.Generic;
using NXG.Models;

namespace NXG.Services;

public interface ILoggingService
{
    void Log(string action, string details = "", string level = "Info", string tweakId = "");
    void LogWarning(string action, string details = "");
    void LogError(string action, string details = "", string tweakId = "");
    void LogRollback(string action, string details = "", string tweakId = "");
    List<LogEntry> GetLogs();
    void ClearLogs();
    string ExportToJson();
    string ExportToCsv();
}
