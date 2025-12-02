using VideoSubtitleGenerator.Core.Enums;

namespace VideoSubtitleGenerator.Core.Models;

/// <summary>
/// Application configuration settings
/// </summary>
public class AppSettings
{
    public PythonSettings Python { get; set; } = new();
    
    public FFmpegSettings FFmpeg { get; set; } = new();
    
    public WhisperSettings Whisper { get; set; } = new();
    
    public ProcessingSettings Processing { get; set; } = new();
    
    public LoggingSettings Logging { get; set; } = new();
}

public class PythonSettings
{
    public string PythonExePath { get; set; } = "python.exe";
    
    public string ScriptPath { get; set; } = "python-worker\\process_media.py";
    
    public string? VenvPath { get; set; }
}

public class FFmpegSettings
{
    public bool UseBundled { get; set; } = true;
    
    public string ExecutablePath { get; set; } = "ffmpeg\\bin\\ffmpeg.exe";
}

public class ProcessingSettings
{
    public ProcessingMode DefaultMode { get; set; } = ProcessingMode.Sequential;
    
    public int MaxParallelJobs { get; set; } = 2;
    
    public bool AutoDeleteWavFiles { get; set; } = true;
    
    // Empty = save subtitles in same folder as video (recommended)
    public string OutputDirectory { get; set; } = string.Empty;
}

public class LoggingSettings
{
    public string LogFilePath { get; set; } = "logs\\app.log";
    
    public string MinimumLevel { get; set; } = "Information";
}
