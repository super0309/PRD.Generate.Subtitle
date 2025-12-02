using System.Diagnostics;
using System.Text;
using System.Text.Json;
using VideoSubtitleGenerator.Core;
using VideoSubtitleGenerator.Core.Enums;
using VideoSubtitleGenerator.Core.Interfaces;
using VideoSubtitleGenerator.Core.Models;

namespace VideoSubtitleGenerator.Infrastructure.Services;

/// <summary>
/// Service for executing Python worker process
/// </summary>
public class PythonWorkerService : IPythonWorkerService
{
    private readonly ILogService _logService;
    private readonly ISettingsService _settingsService;
    private AppSettings _settings;

    public PythonWorkerService(ILogService logService, ISettingsService settingsService)
    {
        _logService = logService;
        _settingsService = settingsService;
        _settings = _settingsService.LoadSettings();
    }

    public async Task<TranscriptionResult> ProcessAsync(
        TranscriptionJob job,
        IProgress<JobProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.Now;

        try
        {
            _logService.LogInfo($"Starting processing: {Path.GetFileName(job.InputFilePath)}");

            // Prepare arguments
            var args = BuildPythonArguments(job);

            // Start Python process
            var processStartInfo = new ProcessStartInfo
            {
                FileName = _settings.Python.PythonExePath,
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = processStartInfo };
            
            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();

            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    outputBuilder.AppendLine(e.Data);
                    
                    // Try to parse progress updates
                    if (e.Data.StartsWith("{") && progress != null)
                    {
                        try
                        {
                            var progressUpdate = JsonSerializer.Deserialize<ProgressMessage>(e.Data);
                            if (progressUpdate != null)
                            {
                                progress.Report(new JobProgress
                                {
                                    Phase = MapPhase(progressUpdate.Phase),
                                    Percent = progressUpdate.Percent,
                                    Message = progressUpdate.Message
                                });
                            }
                        }
                        catch
                        {
                            // Not a JSON progress message, just log it
                            _logService.LogDebug(e.Data);
                        }
                    }
                }
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    errorBuilder.AppendLine(e.Data);
                    _logService.LogWarning($"Python stderr: {e.Data}");
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            // Wait for process to complete or cancellation
            var completionTask = Task.Run(() => process.WaitForExit(), cancellationToken);
            await completionTask;

            cancellationToken.ThrowIfCancellationRequested();

            var duration = DateTime.Now - startTime;

            if (process.ExitCode == 0)
            {
                _logService.LogInfo($"Processing completed: {Path.GetFileName(job.InputFilePath)}");

                // Parse result from output
                var result = ParseResult(outputBuilder.ToString(), job);
                result.Duration = duration;
                return result;
            }
            else
            {
                var error = errorBuilder.ToString();
                _logService.LogError($"Processing failed with exit code {process.ExitCode}: {error}");

                return new TranscriptionResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Python process failed with exit code {process.ExitCode}: {error}",
                    Duration = duration
                };
            }
        }
        catch (OperationCanceledException)
        {
            _logService.LogInfo($"Processing cancelled: {Path.GetFileName(job.InputFilePath)}");
            throw;
        }
        catch (Exception ex)
        {
            Utilities.WriteToLog(ex);
            _logService.LogError($"Processing error: {Path.GetFileName(job.InputFilePath)}", ex);

            return new TranscriptionResult
            {
                IsSuccess = false,
                ErrorMessage = ex.Message,
                Duration = DateTime.Now - startTime
            };
        }
    }

    public async Task<bool> ValidateEnvironmentAsync()
    {
        try
        {
            // Check Python executable
            var pythonVersion = GetPythonVersion();
            if (string.IsNullOrEmpty(pythonVersion))
            {
                _logService.LogError("Python executable not found");
                return false;
            }

            // Check FFmpeg
            var ffmpegCheck = await RunCommandAsync(_settings.FFmpeg.ExecutablePath, "-version");
            if (!ffmpegCheck.success)
            {
                _logService.LogError("FFmpeg not found");
                return false;
            }

            // Check worker script exists
            if (!File.Exists(_settings.Python.ScriptPath))
            {
                _logService.LogError($"Worker script not found: {_settings.Python.ScriptPath}");
                return false;
            }

            _logService.LogInfo($"Environment validated - Python: {pythonVersion}");
            return true;
        }
        catch (Exception ex)
        {
            Utilities.WriteToLog(ex);
            _logService.LogError("Environment validation failed", ex);
            return false;
        }
    }

    public string GetPythonVersion()
    {
        try
        {
            var (success, output) = RunCommandAsync(_settings.Python.PythonExePath, "--version").Result;
            return success ? output.Trim() : string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private string BuildPythonArguments(TranscriptionJob job)
    {
        try
        {
            var config = new
            {
                input_file = job.InputFilePath,
                output_dir = job.OutputDirectory,
                ffmpeg_path = _settings.FFmpeg.ExecutablePath,
                whisper_model = job.Settings.Model,
                language = job.Settings.Language,
                device = job.Settings.Device,
                fp16 = job.Settings.Fp16,
                task = job.Settings.Task,
                output_format = job.Settings.OutputFormat
            };

            var configJson = JsonSerializer.Serialize(config);
            var configBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(configJson));

            return $"\"{_settings.Python.ScriptPath}\" --config {configBase64}";
        }
        catch (Exception ex)
        {
            Utilities.WriteToLog(ex);
            _logService?.LogError($"Error building Python arguments: {ex.Message}", ex);
            throw;
        }
    }

    private TranscriptionResult ParseResult(string output, TranscriptionJob job)
    {
        try
        {
            // Look for result JSON in output
            var lines = output.Split('\n');
            foreach (var line in lines)
            {
                if (line.TrimStart().StartsWith("{\"result\":"))
                {
                    var resultData = JsonSerializer.Deserialize<ResultMessage>(line);
                    if (resultData?.Result != null)
                    {
                        return new TranscriptionResult
                        {
                            IsSuccess = true,
                            WavFilePath = resultData.Result.WavFile,
                            SubtitleFilePath = resultData.Result.SubtitleFile,
                            Metadata = resultData.Result.Metadata ?? new Dictionary<string, string>()
                        };
                    }
                }
            }

            // If no JSON result found, assume success and construct paths
            var outputFileName = Path.GetFileNameWithoutExtension(job.InputFilePath);
            var wavPath = Path.Combine(job.OutputDirectory, $"{outputFileName}.wav");
            var subtitlePath = Path.Combine(job.OutputDirectory, $"{outputFileName}.{job.Settings.OutputFormat}");

            return new TranscriptionResult
            {
                IsSuccess = true,
                WavFilePath = File.Exists(wavPath) ? wavPath : null,
                SubtitleFilePath = File.Exists(subtitlePath) ? subtitlePath : null
            };
        }
        catch (Exception ex)
        {
            Utilities.WriteToLog(ex);
            _logService.LogWarning($"Failed to parse result JSON: {ex.Message}");
            return new TranscriptionResult { IsSuccess = false, ErrorMessage = "Failed to parse result" };
        }
    }

    private ProcessingPhase MapPhase(string phase)
    {
        try
        {
            return phase.ToLower() switch
            {
                "queued" => ProcessingPhase.Queued,
                "converting" => ProcessingPhase.Converting,
                "transcribing" => ProcessingPhase.Transcribing,
                "finalizing" => ProcessingPhase.Finalizing,
                "completed" => ProcessingPhase.Completed,
                "failed" => ProcessingPhase.Failed,
                _ => ProcessingPhase.Queued
            };
        }
        catch (Exception ex)
        {
            Utilities.WriteToLog(ex);
            _logService?.LogError($"Error mapping phase '{phase}': {ex.Message}", ex);
            return ProcessingPhase.Queued;
        }
    }

    private async Task<(bool success, string output)> RunCommandAsync(string fileName, string arguments)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null)
                return (false, string.Empty);

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            
            await process.WaitForExitAsync();

            return (process.ExitCode == 0, output + error);
        }
        catch
        {
            return (false, string.Empty);
        }
    }

    private class ProgressMessage
    {
        public string Phase { get; set; } = string.Empty;
        public int Percent { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    private class ResultMessage
    {
        public ResultData? Result { get; set; }
    }

    private class ResultData
    {
        public string? WavFile { get; set; }
        public string? SubtitleFile { get; set; }
        public Dictionary<string, string>? Metadata { get; set; }
    }
}
