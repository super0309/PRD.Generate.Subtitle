using System.Diagnostics;
using System.Text.Json;
using VideoSubtitleGenerator.Core;
using VideoSubtitleGenerator.Core.Interfaces;
using VideoSubtitleGenerator.Core.Models;

namespace VideoSubtitleGenerator.Infrastructure.Services;

/// <summary>
/// Settings service using JSON file storage in AppData
/// </summary>
public class SettingsService : ISettingsService
{
    private readonly string _settingsPath;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ILogService _logService;

    public SettingsService(ILogService logService)
    {
        _logService = logService;
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appFolder = Path.Combine(appDataPath, "VideoSubtitleGenerator");
        Directory.CreateDirectory(appFolder);
        
        _settingsPath = Path.Combine(appFolder, "settings.json");
        
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };
    }

    public AppSettings LoadSettings()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                var json = File.ReadAllText(_settingsPath);
                return JsonSerializer.Deserialize<AppSettings>(json, _jsonOptions) 
                    ?? GetDefaultSettings();
            }
        }
        catch
        {
            // If loading fails, return defaults
        }

        return GetDefaultSettings();
    }

    public async Task SaveSettingsAsync(AppSettings settings)
    {
        try
        {
            var json = JsonSerializer.Serialize(settings, _jsonOptions);
            await File.WriteAllTextAsync(_settingsPath, json);
        }
        catch (Exception ex)
        {
            Utilities.WriteToLog(ex);
            _logService?.LogError($"Error saving settings: {ex.Message}");
            throw;
        }
    }

    public AppSettings GetDefaultSettings()
    {
        try
        {
            _logService.LogInfo("=== GetDefaultSettings() called - Starting auto-detection ===");
            
            // Auto-detect paths on first run
            var pythonPath = AutoDetectPython();
            _logService.LogInfo($"AutoDetectPython result: {pythonPath ?? "NULL"}");
            
            var ffmpegPath = AutoDetectFFmpeg();
            _logService.LogInfo($"AutoDetectFFmpeg result: {ffmpegPath ?? "NULL"}");
            
            var scriptPath = AutoDetectScript();
            _logService.LogInfo($"AutoDetectScript result: {scriptPath ?? "NULL"}");
            
            _logService.LogInfo("=== GetDefaultSettings() completed ===");
            
            return new AppSettings
            {
                Python = new PythonSettings
                {
                    PythonExePath = pythonPath ?? "python",
                    ScriptPath = scriptPath ?? "python-worker/process_media.py",
                    VenvPath = null
                },
                FFmpeg = new FFmpegSettings
                {
                    UseBundled = string.IsNullOrEmpty(ffmpegPath), // Use bundled if not found
                    ExecutablePath = ffmpegPath ?? "ffmpeg"
                },
                Whisper = new WhisperSettings
                {
                    Model = "base",
                    Language = "auto",
                    Device = "cpu",
                    Fp16 = false,
                    Task = "transcribe",
                    OutputFormat = "srt"
                },
                Processing = new ProcessingSettings
                {
                    DefaultMode = Core.Enums.ProcessingMode.Sequential,
                    MaxParallelJobs = Math.Max(2, Environment.ProcessorCount / 2),
                    AutoDeleteWavFiles = true,
                    OutputDirectory = string.Empty // Save with video files by default
                },
                Logging = new LoggingSettings
                {
                    LogFilePath = "logs\\app.log",
                    MinimumLevel = "Information"
                }
            };
        }
        catch (Exception ex)
        {
            Utilities.WriteToLog(ex);
            _logService?.LogError($"Error in GetDefaultSettings: {ex.Message}");
            // Return minimal fallback settings
            return new AppSettings
            {
                Python = new PythonSettings { PythonExePath = "python", ScriptPath = "python-worker/process_media.py" },
                FFmpeg = new FFmpegSettings { UseBundled = true, ExecutablePath = "ffmpeg" },
                Whisper = new WhisperSettings { Model = "base", Language = "auto", Device = "cpu" },
                Processing = new ProcessingSettings { DefaultMode = Core.Enums.ProcessingMode.Sequential, MaxParallelJobs = 2 },
                Logging = new LoggingSettings { LogFilePath = "logs\\app.log", MinimumLevel = "Information" }
            };
        }
    }
    
    /// <summary>
    /// Auto-detect Python executable path
    /// </summary>
    private string? AutoDetectPython()
    {
        try
        {
            _logService.LogInfo("Auto-detecting Python...");
            
            // Strategy 1: Check AppData (most common for Windows installer)
            var appDataPython = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Programs", "Python");
            
            _logService.LogInfo($"Checking AppData Python path: {appDataPython}");
            
        if (Directory.Exists(appDataPython))
        {
            try
            {
                // Find all python.exe, sort by version (newest first)
                var pythonExes = Directory.GetFiles(appDataPython, "python.exe", SearchOption.AllDirectories)
                    .OrderByDescending(p => p) // Python313 comes before Python312, etc.
                    .ToList();
                
                _logService.LogInfo($"Found {pythonExes.Count} python.exe files in AppData");
                foreach (var exe in pythonExes)
                {
                    _logService.LogInfo($"  - {exe}");
                }
                    
                foreach (var pythonExe in pythonExes)
                {
                    // Skip pythonw.exe, only want python.exe
                    if (Path.GetFileName(pythonExe).Equals("python.exe", StringComparison.OrdinalIgnoreCase))
                    {
                        _logService.LogInfo($"Selected: {pythonExe}");
                        return pythonExe;
                    }
                }
            }
            catch (Exception ex)
            {
                Utilities.WriteToLog(ex);
                _logService.LogError($"Error searching AppData Python: {ex.Message}");
            }
        }
        else
        {
            _logService.LogInfo($"AppData Python directory does not exist");
        }
        
        // Strategy 2: Check common installation directories
        var commonPaths = new[]
        {
            @"C:\Python313",
            @"C:\Python312",
            @"C:\Python311",
            @"C:\Python310",
            @"C:\Python39",
            @"C:\Python38",
            @"C:\Python3",
            @"C:\Python",
        };
        
        _logService.LogInfo("Checking common installation directories...");
        foreach (var basePath in commonPaths)
        {
            var pythonExe = Path.Combine(basePath, "python.exe");
            if (File.Exists(pythonExe))
            {
                _logService.LogInfo($"Found: {pythonExe}");
                return pythonExe;
            }
        }
        
        // Strategy 3: Check PATH environment variable
        _logService.LogInfo("Checking PATH environment variable...");
        var pathVar = Environment.GetEnvironmentVariable("PATH");
        if (!string.IsNullOrEmpty(pathVar))
        {
            foreach (var path in pathVar.Split(Path.PathSeparator))
            {
                try
                {
                    var pythonExe = Path.Combine(path.Trim(), "python.exe");
                    if (File.Exists(pythonExe))
                    {
                        // Skip Windows Store alias (WindowsApps folder)
                        if (pythonExe.Contains("WindowsApps", StringComparison.OrdinalIgnoreCase))
                        {
                            _logService.LogInfo($"Skipping Windows Store alias: {pythonExe}");
                            continue;
                        }
                        
                        // Validate by running python --version
                        if (ValidatePythonExecutable(pythonExe))
                        {
                            _logService.LogInfo($"Found valid Python in PATH: {pythonExe}");
                            return pythonExe;
                        }
                        else
                        {
                            _logService.LogWarning($"Found but invalid Python: {pythonExe}");
                        }
                    }
                }
                catch { }
            }
        }
        
        _logService.LogWarning("Python not found in any location");
        return null;
        }
        catch (Exception ex)
        {
            Utilities.WriteToLog(ex);
            _logService?.LogError($"Error in AutoDetectPython: {ex.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// Auto-detect FFmpeg executable path
    /// </summary>
    private string? AutoDetectFFmpeg()
    {
        try
        {
            // Check common locations
        var commonPaths = new[]
        {
            @"C:\ffmpeg\bin\ffmpeg.exe",
            @"C:\Program Files\ffmpeg\bin\ffmpeg.exe",
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg", "bin", "ffmpeg.exe"),
        };
        
        foreach (var path in commonPaths)
        {
            if (File.Exists(path))
            {
                return path;
            }
        }
        
        // Try to find in PATH
        var pathVar = Environment.GetEnvironmentVariable("PATH");
        if (!string.IsNullOrEmpty(pathVar))
        {
            foreach (var path in pathVar.Split(Path.PathSeparator))
            {
                try
                {
                    var ffmpegExe = Path.Combine(path, "ffmpeg.exe");
                    if (File.Exists(ffmpegExe))
                    {
                        return ffmpegExe;
                    }
                }
                catch { }
            }
        }
        
        return null;
        }
        catch (Exception ex)
        {
            Utilities.WriteToLog(ex);
            _logService?.LogError($"Error in AutoDetectFFmpeg: {ex.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// Auto-detect Python worker script path
    /// </summary>
    private string? AutoDetectScript()
    {
        try
        {
            var basePath = AppDomain.CurrentDomain.BaseDirectory;
        
        // Try relative path from app directory
        var relativePath = Path.Combine(basePath, "python-worker", "process_media.py");
        if (File.Exists(relativePath))
        {
            return relativePath;
        }
        
        // Try going up directories (for development environment)
        var currentDir = new DirectoryInfo(basePath);
        for (int i = 0; i < 5; i++)
        {
            if (currentDir == null || !currentDir.Exists)
                break;
                
            var scriptPath = Path.Combine(currentDir.FullName, "python-worker", "process_media.py");
            if (File.Exists(scriptPath))
            {
                return scriptPath;
            }
            
            currentDir = currentDir.Parent;
        }
        
        return null;
        }
        catch (Exception ex)
        {
            Utilities.WriteToLog(ex);
            _logService?.LogError($"Error in AutoDetectScript: {ex.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// Validate Python executable by running --version
    /// </summary>
    private bool ValidatePythonExecutable(string pythonPath)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = pythonPath,
                Arguments = "--version",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            
            using var process = Process.Start(startInfo);
            if (process == null)
                return false;
                
            process.WaitForExit(3000); // 3 second timeout
            
            // Exit code 0 means success
            if (process.ExitCode == 0)
            {
                var output = process.StandardOutput.ReadToEnd();
                _logService.LogDebug($"Python validation output: {output}");
                return true;
            }
            
            return false;
        }
        catch (Exception ex)
        {
            _logService.LogDebug($"Python validation failed for {pythonPath}: {ex.Message}");
            return false;
        }
    }
}
