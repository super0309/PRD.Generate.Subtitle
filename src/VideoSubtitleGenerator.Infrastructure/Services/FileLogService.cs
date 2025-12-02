using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using VideoSubtitleGenerator.Core;
using VideoSubtitleGenerator.Core.Interfaces;

namespace VideoSubtitleGenerator.Infrastructure.Services;

/// <summary>
/// File-based logging service
/// Reads log directory from appsettings.json or defaults to application directory
/// </summary>
public class FileLogService : ILogService
{
    private readonly string _logDirectory;
    private readonly ConcurrentQueue<LogEntry> _recentLogs;
    private readonly int _maxRecentLogs = 1000;
    private readonly object _fileLock = new();

    public FileLogService()
    {
        _logDirectory = GetLogDirectory();
        Directory.CreateDirectory(_logDirectory);
        
        _recentLogs = new ConcurrentQueue<LogEntry>();
        
        // Log where we're saving logs
        LogInfo($"Log directory: {_logDirectory}");
    }
    
    /// <summary>
    /// Get log directory from appsettings.json or use default
    /// </summary>
    private string GetLogDirectory()
    {
        try
        {
            // Try to read appsettings.json
            var appSettingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
            
            if (File.Exists(appSettingsPath))
            {
                var json = File.ReadAllText(appSettingsPath);
                var config = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
                
                if (config != null && config.TryGetValue("Logging", out var loggingSection))
                {
                    if (loggingSection.TryGetProperty("LogDirectory", out var logDirValue))
                    {
                        var logDir = logDirValue.GetString();
                        
                        if (!string.IsNullOrWhiteSpace(logDir))
                        {
                            // Use configured path (can be absolute or relative)
                            if (Path.IsPathRooted(logDir))
                            {
                                return logDir;
                            }
                            else
                            {
                                // Relative path - combine with app directory
                                return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, logDir);
                            }
                        }
                    }
                }
            }
        }
        catch
        {
            // If config reading fails, fall through to default
        }
        
        // Default: Logs folder next to exe
        return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
    }

    public void LogInfo(string message)
    {
        try
        {
            Log(LogLevel.Info, message, null);
        }
        catch (Exception ex)
        {
            // Last resort - write to console if log system fails
            Console.WriteLine($"[LogInfo FAILED] {ex.Message}");
        }
    }

    public void LogWarning(string message)
    {
        try
        {
            Log(LogLevel.Warning, message, null);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LogWarning FAILED] {ex.Message}");
        }
    }

    public void LogError(string message, Exception? exception = null)
    {
        try
        {
            Log(LogLevel.Error, message, exception);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LogError FAILED] {ex.Message}");
        }
    }

    public void LogDebug(string message)
    {
        try
        {
            Log(LogLevel.Debug, message, null);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LogDebug FAILED] {ex.Message}");
        }
    }

    public IEnumerable<LogEntry> GetRecentLogs(int count = 100)
    {
        try
        {
            return _recentLogs.TakeLast(count);
        }
        catch (Exception ex)
        {
            Utilities.WriteToLog(ex);
            return Enumerable.Empty<LogEntry>();
        }
    }

    public async Task ClearLogsAsync()
    {
        try
        {
            _recentLogs.Clear();
            
            await Task.Run(() =>
            {
                var logFiles = Directory.GetFiles(_logDirectory, "*.log");
                foreach (var file in logFiles)
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch
                    {
                        // Ignore deletion errors
                    }
                }
            });
        }
        catch (Exception ex)
        {
            Utilities.WriteToLog(ex);
            LogError($"Error clearing logs: {ex.Message}", ex);
        }
    }

    private void Log(LogLevel level, string message, Exception? exception)
    {
        try
        {
            var entry = new LogEntry
            {
                Timestamp = DateTime.Now,
                Level = level,
                Message = message,
                Exception = exception?.ToString()
            };

            // Add to recent logs
            _recentLogs.Enqueue(entry);
            while (_recentLogs.Count > _maxRecentLogs)
            {
                _recentLogs.TryDequeue(out _);
            }

            // Write to file
            WriteToFile(entry);
        }
        catch (Exception ex)
        {
            // Last resort - try to write to console
            Console.WriteLine($"[CRITICAL LOG FAILURE] {ex.Message}: Original message was: {message}");
        }
    }

    private void WriteToFile(LogEntry entry)
    {
        try
        {
            var logFileName = $"log_{DateTime.Now:yyyyMMdd}.log";
            var logFilePath = Path.Combine(_logDirectory, logFileName);

            var logLine = $"[{entry.Timestamp:yyyy-MM-dd HH:mm:ss}] [{entry.Level}] {entry.Message}";
            if (!string.IsNullOrEmpty(entry.Exception))
            {
                logLine += $"\n{entry.Exception}";
            }
            logLine += Environment.NewLine;

            lock (_fileLock)
            {
                File.AppendAllText(logFilePath, logLine);
            }

            // Clean old log files (keep last 10 days)
            CleanOldLogs();
        }
        catch
        {
            // Fail silently for logging errors
        }
    }

    private void CleanOldLogs()
    {
        try
        {
            var logFiles = Directory.GetFiles(_logDirectory, "*.log")
                .Select(f => new FileInfo(f))
                .Where(f => f.LastWriteTime < DateTime.Now.AddDays(-10))
                .ToList();

            foreach (var file in logFiles)
            {
                file.Delete();
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }
}
