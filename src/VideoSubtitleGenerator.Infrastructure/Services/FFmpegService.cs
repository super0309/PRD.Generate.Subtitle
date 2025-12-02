using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using VideoSubtitleGenerator.Core;
using VideoSubtitleGenerator.Core.Interfaces;

namespace VideoSubtitleGenerator.Infrastructure.Services;

/// <summary>
/// Service for FFmpeg operations - video to audio conversion
/// </summary>
public class FFmpegService : IFFmpegService
{
    private readonly ILogService _logger;
    private readonly ISettingsService _settings;
    private string? _ffmpegPath;

    public FFmpegService(ILogService logger, ISettingsService settings)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    /// <summary>
    /// Converts video file to WAV audio format
    /// </summary>
    public async Task<string> ConvertToWavAsync(
        string videoPath,
        string outputPath,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(videoPath))
            throw new ArgumentException("Video path cannot be empty", nameof(videoPath));

        if (!File.Exists(videoPath))
            throw new FileNotFoundException($"Video file not found: {videoPath}");

        // Ensure FFmpeg is available
        await EnsureFFmpegPathAsync();

        // Get video duration for progress calculation
        var duration = await GetDurationAsync(videoPath, cancellationToken);
        
        _logger.LogInfo($"Converting video to WAV: {Path.GetFileName(videoPath)}");
        _logger.LogDebug($"Video duration: {duration.TotalSeconds:F2} seconds");

        // Build FFmpeg arguments
        var wavPath = string.IsNullOrEmpty(outputPath) 
            ? Path.ChangeExtension(videoPath, ".wav") 
            : outputPath;

        // Ensure output directory exists
        var outputDir = Path.GetDirectoryName(wavPath);
        if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }

        // FFmpeg command: -i input.mp4 -ar 16000 -ac 1 -y output.wav
        var arguments = $"-i \"{videoPath}\" -ar 16000 -ac 1 -y \"{wavPath}\"";

        var startInfo = new ProcessStartInfo
        {
            FileName = _ffmpegPath,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = Directory.GetCurrentDirectory()
        };

        using var process = new Process { StartInfo = startInfo };
        var errorOutput = string.Empty;

        // Subscribe to stderr for progress
        process.ErrorDataReceived += (sender, e) =>
        {
            if (string.IsNullOrEmpty(e.Data)) return;

            errorOutput += e.Data + "\n";

            // Parse progress from FFmpeg output
            // Format: time=00:01:23.45
            var match = Regex.Match(e.Data, @"time=(\d{2}):(\d{2}):(\d{2}\.\d{2})");
            if (match.Success && duration.TotalSeconds > 0)
            {
                var hours = int.Parse(match.Groups[1].Value);
                var minutes = int.Parse(match.Groups[2].Value);
                var seconds = double.Parse(match.Groups[3].Value);
                var currentTime = TimeSpan.FromHours(hours).Add(TimeSpan.FromMinutes(minutes)).Add(TimeSpan.FromSeconds(seconds));

                var percentage = (int)((currentTime.TotalSeconds / duration.TotalSeconds) * 100);
                progress?.Report(Math.Min(percentage, 100));
            }
        };

        try
        {
            _logger.LogDebug($"Executing: {_ffmpegPath} {arguments}");
            
            process.Start();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode != 0)
            {
                _logger.LogError($"FFmpeg conversion failed with exit code {process.ExitCode}");
                _logger.LogError($"FFmpeg error output: {errorOutput}");
                throw new InvalidOperationException($"FFmpeg conversion failed: {errorOutput}");
            }

            if (!File.Exists(wavPath))
            {
                throw new InvalidOperationException($"WAV file was not created: {wavPath}");
            }

            progress?.Report(100);
            _logger.LogInfo($"Conversion completed: {Path.GetFileName(wavPath)}");
            
            return wavPath;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Conversion was cancelled");
            
            // Clean up partial file
            if (File.Exists(wavPath))
            {
                try { File.Delete(wavPath); } catch { }
            }
            
            throw;
        }
        catch (Exception ex)
        {
            Utilities.WriteToLog(ex);
            _logger.LogError($"Error during FFmpeg conversion: {ex.Message}", ex);
            throw;
        }
    }

    /// <summary>
    /// Gets video duration in seconds
    /// </summary>
    public async Task<TimeSpan> GetDurationAsync(string videoPath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(videoPath))
            throw new FileNotFoundException($"Video file not found: {videoPath}");

        await EnsureFFmpegPathAsync();

        // Use ffprobe or ffmpeg to get duration
        // ffmpeg -i input.mp4 2>&1 | grep "Duration"
        var arguments = $"-i \"{videoPath}\"";

        var startInfo = new ProcessStartInfo
        {
            FileName = _ffmpegPath,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo };
        var output = string.Empty;

        process.ErrorDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                output += e.Data + "\n";
        };

        try
        {
            process.Start();
            process.BeginErrorReadLine();
            await process.WaitForExitAsync(cancellationToken);

            // Parse duration from output
            // Format: Duration: 00:01:23.45
            var match = Regex.Match(output, @"Duration:\s*(\d{2}):(\d{2}):(\d{2}\.\d{2})");
            if (match.Success)
            {
                var hours = int.Parse(match.Groups[1].Value);
                var minutes = int.Parse(match.Groups[2].Value);
                var seconds = double.Parse(match.Groups[3].Value);
                return TimeSpan.FromHours(hours).Add(TimeSpan.FromMinutes(minutes)).Add(TimeSpan.FromSeconds(seconds));
            }

            _logger.LogWarning($"Could not parse video duration from: {output}");
            return TimeSpan.Zero;
        }
        catch (Exception ex)
        {
            Utilities.WriteToLog(ex);
            _logger.LogError($"Error getting video duration: {ex.Message}", ex);
            return TimeSpan.Zero;
        }
    }

    /// <summary>
    /// Gets media information (format, codec, resolution, etc.)
    /// </summary>
    public async Task<string> GetMediaInfoAsync(string videoPath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(videoPath))
            throw new FileNotFoundException($"Video file not found: {videoPath}");

        await EnsureFFmpegPathAsync();

        var arguments = $"-i \"{videoPath}\" -hide_banner";

        var startInfo = new ProcessStartInfo
        {
            FileName = _ffmpegPath,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo };
        var output = string.Empty;

        process.ErrorDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                output += e.Data + "\n";
        };

        try
        {
            process.Start();
            process.BeginErrorReadLine();
            await process.WaitForExitAsync(cancellationToken);

            return output;
        }
        catch (Exception ex)
        {
            Utilities.WriteToLog(ex);
            _logger.LogError($"Error getting media info: {ex.Message}", ex);
            return string.Empty;
        }
    }

    /// <summary>
    /// Validates FFmpeg installation
    /// </summary>
    public async Task<bool> ValidateInstallationAsync()
    {
        try
        {
            await EnsureFFmpegPathAsync();

            if (string.IsNullOrEmpty(_ffmpegPath) || !File.Exists(_ffmpegPath))
            {
                _logger.LogError($"FFmpeg not found at: {_ffmpegPath}");
                return false;
            }

            // Test FFmpeg by running version command
            var startInfo = new ProcessStartInfo
            {
                FileName = _ffmpegPath,
                Arguments = "-version",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };
            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode == 0 && output.Contains("ffmpeg version"))
            {
                _logger.LogInfo($"FFmpeg validated successfully: {_ffmpegPath}");
                return true;
            }

            _logger.LogError("FFmpeg validation failed");
            return false;
        }
        catch (Exception ex)
        {
            Utilities.WriteToLog(ex);
            _logger.LogError($"FFmpeg validation error: {ex.Message}", ex);
            return false;
        }
    }

    /// <summary>
    /// Ensures FFmpeg path is set and valid
    /// </summary>
    private Task EnsureFFmpegPathAsync()
    {
        try
        {
            if (!string.IsNullOrEmpty(_ffmpegPath))
                return Task.CompletedTask;

            // Try to get from settings
            var settings = _settings.LoadSettings();
            _ffmpegPath = settings?.FFmpeg?.ExecutablePath;

            // If not in settings, try to find in PATH or common locations
            if (string.IsNullOrEmpty(_ffmpegPath) || !File.Exists(_ffmpegPath))
            {
                _ffmpegPath = FindFFmpegInPath() ?? FindFFmpegInCommonLocations();
            }

            if (string.IsNullOrEmpty(_ffmpegPath))
            {
                throw new InvalidOperationException(
                    "FFmpeg not found. Please install FFmpeg or set the path in Settings.");
            }

            _logger.LogDebug($"Using FFmpeg at: {_ffmpegPath}");
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            Utilities.WriteToLog(ex);
            _logger?.LogError($"Error ensuring FFmpeg path: {ex.Message}", ex);
            throw;
        }
    }

    /// <summary>
    /// Tries to find FFmpeg in system PATH
    /// </summary>
    private string? FindFFmpegInPath()
    {
        try
        {
            var pathEnv = Environment.GetEnvironmentVariable("PATH");
            if (string.IsNullOrEmpty(pathEnv)) return null;

            var paths = pathEnv.Split(Path.PathSeparator);
            foreach (var path in paths)
            {
                var ffmpegPath = Path.Combine(path, "ffmpeg.exe");
                if (File.Exists(ffmpegPath))
                {
                    _logger.LogDebug($"Found FFmpeg in PATH: {ffmpegPath}");
                    return ffmpegPath;
                }
            }
        }
        catch (Exception ex)
        {
            Utilities.WriteToLog(ex);
            _logger.LogWarning($"Error searching PATH for FFmpeg: {ex.Message}");
        }

        return null;
    }

    /// <summary>
    /// Tries to find FFmpeg in common installation locations
    /// </summary>
    private string? FindFFmpegInCommonLocations()
    {
        try
        {
            var commonPaths = new[]
            {
                @"C:\ffmpeg\bin\ffmpeg.exe",
                @"C:\Program Files\ffmpeg\bin\ffmpeg.exe",
                @"C:\Program Files (x86)\ffmpeg\bin\ffmpeg.exe",
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg", "ffmpeg.exe"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg.exe")
            };

            foreach (var path in commonPaths)
            {
                if (File.Exists(path))
                {
                    _logger.LogDebug($"Found FFmpeg at common location: {path}");
                    return path;
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            Utilities.WriteToLog(ex);
            _logger?.LogWarning($"Error searching common locations for FFmpeg: {ex.Message}");
            return null;
        }
    }
}
