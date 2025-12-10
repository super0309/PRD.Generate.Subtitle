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
            _logService.LogInfo("============================================================");
            _logService.LogInfo($"🚀 PythonWorkerService.ProcessAsync CALLED");
            _logService.LogInfo($"📁 Input file: {job.InputFilePath}");
            _logService.LogInfo($"📝 Job ID: {job.Id}");
            _logService.LogInfo($"⏰ Start time: {startTime:yyyy-MM-dd HH:mm:ss}");
            _logService.LogInfo($"📊 Progress reporter: {(progress == null ? "NULL" : "NOT NULL")}");
            _logService.LogInfo("============================================================");
            _logService.LogInfo($"Starting processing: {Path.GetFileName(job.InputFilePath)}");

            // Prepare arguments
            var args = BuildPythonArguments(job);

            // Start Python process
            var processStartInfo = new ProcessStartInfo
            {
                FileName = _settings.Python.PythonExePath,
                Arguments = $"-u {args}",  // -u flag for unbuffered output
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
                    
                    _logService.LogDebug($"📥 STDOUT: {e.Data}");
                    
                    // Try to parse progress updates
                    if (e.Data.StartsWith("{") && progress != null)
                    {
                        try
                        {
                            _logService.LogDebug($"🔍 Attempting to parse JSON: {e.Data}");

                            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                            
                            // Parse to PythonProcessJob and save to file
                            var pythonJob = JsonSerializer.Deserialize<PythonProcessJob>(e.Data, options);
                            if (pythonJob != null)
                            {
                                // Set job_id from current job
                                pythonJob.job_id = job.Id.GetHashCode(); // Convert Guid to int
                                
                                // Save to file in same directory as log
                                try
                                {
                                    // Get log directory from settings or use default
                                    var logPath = _settings.Logging?.LogFilePath ?? "logs\\app.log";
                                    var logDir = Path.GetDirectoryName(logPath);
                                    
                                    if (string.IsNullOrEmpty(logDir) || !Path.IsPathRooted(logDir))
                                    {
                                        logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
                                    }
                                    
                                    // Ensure directory exists
                                    Directory.CreateDirectory(logDir);
                                    
                                    var jobFile = Path.Combine(logDir, $"job_{job.Id}_process.json");
                                    var jsonContent = JsonSerializer.Serialize(pythonJob, new JsonSerializerOptions 
                                    { 
                                        WriteIndented = true,
                                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                                    });
                                    
                                    // Overwrite file with new data
                                    File.WriteAllText(jobFile, jsonContent);
                                    _logService.LogDebug($"💾 Saved progress to: {jobFile}");
                                }
                                catch (Exception saveEx)
                                {
                                    _logService.LogWarning($"⚠️  Failed to save job file: {saveEx.Message}");
                                }
                                
                                // Report progress to UI
                                _logService.LogInfo($"✅ Progress parsed: {pythonJob.phase} {pythonJob.percent}%");
                                progress.Report(new JobProgress
                                {
                                    Phase = MapPhase(pythonJob.phase),
                                    Percent = pythonJob.percent,
                                    Message = pythonJob.message
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            // Not a JSON progress message, just log it
                            _logService.LogDebug($"❌ JSON parse failed: {ex.Message}");
                        }
                    }
                    else if (e.Data.StartsWith("{"))
                    {
                        _logService.LogWarning($"⚠️  JSON detected but progress is NULL");
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

            // Set up log monitoring thread for Whisper progress (from stderr)
            var logMonitorCts = new CancellationTokenSource();
            var logDir = Path.GetDirectoryName(_settings.Logging?.LogFilePath ?? "logs\\app.log");
            if (string.IsNullOrEmpty(logDir) || !Path.IsPathRooted(logDir))
            {
                logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            }
            
            var jobProcessFile = Path.Combine(logDir, $"job_{job.Id}_process.json");
            
            // Start background thread to monitor log file for Whisper progress
            var logMonitorThread = new Thread(() => MonitorLogForWhisperProgress(job.Id, logDir, jobProcessFile, logMonitorCts.Token))
            {
                IsBackground = true,
                Name = $"LogMonitor-{job.Id}"
            };
            logMonitorThread.Start();

            // Set up file-based progress polling as reliable fallback
            // Progress file is written to the same directory as the Python script
            var scriptDir = Path.GetDirectoryName(Path.GetFullPath(_settings.Python.ScriptPath)) ?? 
                           Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "python-worker");
            var progressFile = Path.Combine(scriptDir, $"{job.Id}_progress.json");
            
            _logService.LogDebug($"📝 Polling progress file at: {progressFile}");
            
            var progressTimer = new System.Timers.Timer(500); // Poll every 500ms
            progressTimer.Elapsed += (s, e) =>
            {
                try
                {
                    if (File.Exists(progressFile))
                    {
                        var json = File.ReadAllText(progressFile);
                        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                        var progressData = JsonSerializer.Deserialize<ProgressMessage>(json, options);
                        if (progressData != null && progress != null)
                        {
                            _logService.LogDebug($"📊 Progress from file: {progressData.Phase} {progressData.Percent}%");
                            progress.Report(new JobProgress
                            {
                                Phase = MapPhase(progressData.Phase),
                                Percent = progressData.Percent,
                                Message = progressData.Message
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Ignore errors (file might be locked during write)
                    _logService.LogDebug($"⚠️  Progress file read error: {ex.Message}");
                }
            };
            progressTimer.Start();

            // Wait for process to complete or cancellation
            var completionTask = Task.Run(() => process.WaitForExit(), cancellationToken);
            await completionTask;

            // Stop log monitor thread
            logMonitorCts.Cancel();
            if (!logMonitorThread.Join(TimeSpan.FromSeconds(2)))
            {
                _logService.LogWarning("⚠️  Log monitor thread did not stop gracefully");
            }
            logMonitorCts.Dispose();

            // Stop timer and cleanup progress file
            progressTimer.Stop();
            progressTimer.Dispose();
            try
            {
                if (File.Exists(progressFile))
                {
                    File.Delete(progressFile);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }

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
                job_id = job.Id.ToString(),
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

            // Check if debugger should be enabled via environment variable
            var enableDebug = Environment.GetEnvironmentVariable("PYTHON_DEBUG") == "1";
            var debugFlag = enableDebug ? " --debug" : "";

            return $"\"{_settings.Python.ScriptPath}\" --config {configBase64}{debugFlag}";
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
                        // Validate subtitle file exists and has content
                        var subtitleFile = resultData.Result.SubtitleFile;
                        if (!string.IsNullOrEmpty(subtitleFile) && File.Exists(subtitleFile))
                        {
                            var fileInfo = new FileInfo(subtitleFile);
                            if (fileInfo.Length == 0)
                            {
                                _logService.LogError($"❌ Subtitle file is empty: {subtitleFile}");
                                return new TranscriptionResult
                                {
                                    IsSuccess = false,
                                    ErrorMessage = "Subtitle file was created but is empty (0 bytes)",
                                    SubtitleFilePath = subtitleFile
                                };
                            }
                            
                            // Optionally: Validate SRT format (has at least one subtitle block)
                            try
                            {
                                var content = File.ReadAllText(subtitleFile);
                                if (!content.Contains("-->"))
                                {
                                    _logService.LogError($"❌ Subtitle file has no valid subtitle entries: {subtitleFile}");
                                    return new TranscriptionResult
                                    {
                                        IsSuccess = false,
                                        ErrorMessage = "Subtitle file created but contains no valid subtitle entries",
                                        SubtitleFilePath = subtitleFile
                                    };
                                }
                            }
                            catch (Exception ex)
                            {
                                _logService.LogWarning($"⚠️  Could not validate subtitle content: {ex.Message}");
                            }
                        }
                        else
                        {
                            _logService.LogError($"❌ Subtitle file not found: {subtitleFile}");
                            return new TranscriptionResult
                            {
                                IsSuccess = false,
                                ErrorMessage = $"Subtitle file was not created: {subtitleFile}"
                            };
                        }
                        
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

            // If no JSON result found, construct paths and validate
            var outputFileName = Path.GetFileNameWithoutExtension(job.InputFilePath);
            var wavPath = Path.Combine(job.OutputDirectory, $"{outputFileName}.wav");
            var subtitlePath = Path.Combine(job.OutputDirectory, $"{outputFileName}.{job.Settings.OutputFormat}");

            // Check if subtitle file exists and has content
            if (File.Exists(subtitlePath))
            {
                var fileInfo = new FileInfo(subtitlePath);
                if (fileInfo.Length == 0)
                {
                    return new TranscriptionResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "Subtitle file is empty (0 bytes)",
                        SubtitleFilePath = subtitlePath
                    };
                }
            }
            else
            {
                return new TranscriptionResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Subtitle file was not created",
                    SubtitleFilePath = null
                };
            }

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

    private ProcessingPhase MapPhase(string? phase)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(phase))
            {
                _logService?.LogWarning("⚠️  MapPhase received null or empty phase");
                return ProcessingPhase.Queued;
            }

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

    private void MonitorLogForWhisperProgress(Guid jobId, string logDir, string jobProcessFile, CancellationToken cancellationToken)
    {
        try
        {
            _logService?.LogDebug($"🔍 Log monitor thread started for job {jobId}");
            
            // Find the most recent log file
            var logFiles = Directory.GetFiles(logDir, "*.log")
                .OrderByDescending(f => File.GetLastWriteTime(f))
                .ToList();
            
            if (!logFiles.Any())
            {
                _logService?.LogWarning("⚠️  No log files found to monitor");
                return;
            }
            
            var logFile = logFiles.First();
            _logService?.LogDebug($"📄 Monitoring log file: {logFile}");
            
            var lastPosition = 0L;
            var whisperProgressPattern = @"(\d+)%\|[#\s]+\|\s*(\d+)/(\d+)";
            
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var fileInfo = new FileInfo(logFile);
                    if (fileInfo.Exists && fileInfo.Length > lastPosition)
                    {
                        using var fs = new FileStream(logFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                        fs.Seek(lastPosition, SeekOrigin.Begin);
                        using var sr = new StreamReader(fs);
                        
                        string? line;
                        while ((line = sr.ReadLine()) != null)
                        {
                            // Parse Whisper progress: "  8%|7         | 2548/32347"
                            if (line.Contains("Python stderr:") && line.Contains("%|"))
                            {
                                var match = System.Text.RegularExpressions.Regex.Match(line, whisperProgressPattern);
                                if (match.Success)
                                {
                                    var percent = int.Parse(match.Groups[1].Value);
                                    var current = int.Parse(match.Groups[2].Value);
                                    var total = int.Parse(match.Groups[3].Value);
                                    
                                    // Calculate actual transcription progress (70-95% range)
                                    var transcriptionProgress = 70 + (percent * 25 / 100);
                                    
                                    var pythonJob = new PythonProcessJob
                                    {
                                        phase = "transcribing",
                                        percent = transcriptionProgress,
                                        message = $"Transcribing: {current}/{total} frames ({percent}%)",
                                        job_id = jobId.GetHashCode()
                                    };
                                    
                                    // Save to job process file
                                    var jsonContent = JsonSerializer.Serialize(pythonJob, new JsonSerializerOptions 
                                    { 
                                        WriteIndented = true,
                                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                                    });
                                    File.WriteAllText(jobProcessFile, jsonContent);
                                    
                                    _logService?.LogDebug($"📊 Whisper progress: {percent}% ({current}/{total}) -> {transcriptionProgress}%");
                                }
                            }
                        }
                        
                        lastPosition = fs.Position;
                    }
                    
                    Thread.Sleep(1000); // Check every 1 second
                }
                catch (IOException)
                {
                    // File might be locked, try again
                    Thread.Sleep(500);
                }
            }
            
            _logService?.LogDebug($"🛑 Log monitor thread stopped for job {jobId}");
        }
        catch (Exception ex)
        {
            _logService?.LogError($"❌ Log monitor thread error: {ex.Message}", ex);
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
