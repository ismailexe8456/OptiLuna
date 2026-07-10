using System;

namespace Dtrl.Models;

public class LogEntry
{
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public string Level { get; set; } = "Info"; // Info, Warning, Error, Rollback
    public string Action { get; set; } = string.Empty;
    public string TweakId { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;

    public string LogTimeText => Timestamp.ToString("yyyy-MM-dd HH:mm:ss");
}
