namespace VideoSubtitleGenerator.Core.Interfaces;

/// <summary>
/// Service for application logging
/// </summary>
public interface ILogService
{
    void LogInfo(string message);
    void LogWarning(string message);
    void LogError(string message, Exception? exception = null);
    void LogDebug(string message);
    
    IEnumerable<LogEntry> GetRecentLogs(int count = 100);
    Task ClearLogsAsync();
}

public class LogEntry
{
    public DateTime Timestamp { get; set; }
    public LogLevel Level { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Exception { get; set; }
}

public enum LogLevel
{
    Debug,
    Info,
    Warning,
    Error
}
