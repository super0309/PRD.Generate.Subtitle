using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using VideoSubtitleGenerator.Core;
using VideoSubtitleGenerator.Core.Enums;
using VideoSubtitleGenerator.Core.Interfaces;
using VideoSubtitleGenerator.Core.Models;

namespace VideoSubtitleGenerator.Infrastructure.Services;

/// <summary>
/// Orchestrates job processing workflow - coordinates FFmpeg, Python Worker, and job queue
/// </summary>
public class JobOrchestrator : IJobOrchestrator
{
    private readonly IJobQueueService _jobQueue;
    private readonly IFFmpegService _ffmpeg;
    private readonly IPythonWorkerService _pythonWorker;
    private readonly ILogService _logger;
    private readonly ISettingsService _settings;
    
    private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _runningJobs = new();
    private CancellationTokenSource? _globalCts;
    private bool _isProcessing;
    private bool _isPaused;
    private ProcessingMode _currentMode;
    private int _maxParallelJobs = 2;
    private SemaphoreSlim? _pauseSemaphore;

    // Events
    public event EventHandler<JobProgressEventArgs>? ProgressChanged;
    public event EventHandler<JobCompletedEventArgs>? JobCompleted;

    public bool IsRunning => _isProcessing;
    public int ActiveWorkers => _runningJobs.Count;

    public JobOrchestrator(
        IJobQueueService jobQueue,
        IFFmpegService ffmpeg,
        IPythonWorkerService pythonWorker,
        ILogService logger,
        ISettingsService settings)
    {
        _jobQueue = jobQueue ?? throw new ArgumentNullException(nameof(jobQueue));
        _ffmpeg = ffmpeg ?? throw new ArgumentNullException(nameof(ffmpeg));
        _pythonWorker = pythonWorker ?? throw new ArgumentNullException(nameof(pythonWorker));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    public async Task StartProcessingAsync(ProcessingMode mode, CancellationToken cancellationToken = default)
    {
        if (_isProcessing)
        {
            _logger.LogWarning("Processing already running");
            return;
        }

        try
        {
            _isProcessing = true;
            _isPaused = false;
            _currentMode = mode;
            _globalCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            _logger.LogInfo($"Starting job orchestration in {mode} mode");

            // Validate FFmpeg installation
            var ffmpegValid = await _ffmpeg.ValidateInstallationAsync();
            if (!ffmpegValid)
            {
                throw new InvalidOperationException("FFmpeg is not installed or not accessible");
            }

            // Load settings
            var settings = _settings.LoadSettings();
            _maxParallelJobs = settings?.Processing?.MaxParallelJobs ?? 2;

            // Route to processing method
            if (mode == ProcessingMode.Sequential)
            {
                await ProcessSequentialAsync(_globalCts.Token);
            }
            else
            {
                await ProcessParallelAsync(_maxParallelJobs, _globalCts.Token);
            }

            _logger.LogInfo("Job orchestration completed");
        }
        catch (OperationCanceledException)
        {
            _logger.LogInfo("Job orchestration cancelled");
        }
        catch (Exception ex)
        {
            Utilities.WriteToLog(ex);
            _logger.LogError($"Job orchestration failed: {ex.Message}", ex);
        }
        finally
        {
            _isProcessing = false;
            _globalCts?.Dispose();
            _globalCts = null;
        }
    }

    public Task PauseAsync()
    {
        try
        {
            if (!_isProcessing || _isPaused)
            {
                return Task.CompletedTask;
            }

            _isPaused = true;
            _pauseSemaphore = new SemaphoreSlim(0, 1);
            _logger.LogInfo("Processing paused");
            
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            Utilities.WriteToLog(ex);
            _logger?.LogError($"Error in PauseAsync: {ex.Message}", ex);
            return Task.CompletedTask;
        }
    }

    public Task ResumeAsync()
    {
        try
        {
            if (!_isProcessing || !_isPaused)
            {
                return Task.CompletedTask;
            }

            _isPaused = false;
            _pauseSemaphore?.Release();
            _pauseSemaphore?.Dispose();
            _pauseSemaphore = null;
            _logger.LogInfo("Processing resumed");
            
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            Utilities.WriteToLog(ex);
            _logger?.LogError($"Error in ResumeAsync: {ex.Message}", ex);
            return Task.CompletedTask;
        }
    }

    public async Task CancelAsync()
    {
        try
        {
            if (!_isProcessing)
            {
                return;
            }

            _logger.LogInfo("Cancelling all jobs...");
            
            // Cancel global token
            _globalCts?.Cancel();
            
            // Wait a moment for graceful cancellation
            await Task.Delay(500);
            
            // Force cancel individual jobs if still running
            foreach (var cts in _runningJobs.Values)
            {
                try
                {
                    cts.Cancel();
                }
                catch { }
            }
            
            _runningJobs.Clear();
            _logger.LogInfo("All jobs cancelled");
        }
        catch (Exception ex)
        {
            Utilities.WriteToLog(ex);
            _logger?.LogError($"Error in CancelAsync: {ex.Message}", ex);
        }
    }

    private async Task ProcessSequentialAsync(CancellationToken token)
    {
        try
        {
            _logger.LogInfo("Starting sequential processing");

            while (!token.IsCancellationRequested)
            {
                // Check pause state
                if (_isPaused && _pauseSemaphore != null)
                {
                    await _pauseSemaphore.WaitAsync(token);
                }

                // Get next job from queue
                var job = _jobQueue.DequeueJob();
                if (job == null)
                {
                    _logger.LogDebug("No more jobs in queue");
                    break;
                }

                await ProcessJobAsync(job, token);
            }
        }
        catch (Exception ex)
        {
            Utilities.WriteToLog(ex);
            _logger?.LogError($"Error in ProcessSequentialAsync: {ex.Message}", ex);
            throw;
        }
    }

    private async Task ProcessParallelAsync(int maxParallel, CancellationToken token)
    {
        try
        {
            _logger.LogInfo($"Starting parallel processing with max {maxParallel} workers");

            using var semaphore = new SemaphoreSlim(maxParallel, maxParallel);
            var tasks = new List<Task>();

            while (!token.IsCancellationRequested)
            {
                // Check pause state
                if (_isPaused && _pauseSemaphore != null)
                {
                    await _pauseSemaphore.WaitAsync(token);
                }

                // Get next job
                var job = _jobQueue.DequeueJob();
                if (job == null)
                {
                    break;
                }

                // Wait for available slot
                await semaphore.WaitAsync(token);

                // Start processing in background
                var task = Task.Run(async () =>
                {
                    try
                    {
                        await ProcessJobAsync(job, token);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }, token);

                tasks.Add(task);
            }

            // Wait for all jobs to complete
            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            Utilities.WriteToLog(ex);
            _logger?.LogError($"Error in ProcessParallelAsync: {ex.Message}", ex);
            throw;
        }
    }

    private async Task ProcessJobAsync(TranscriptionJob job, CancellationToken token)
    {
        var jobCts = CancellationTokenSource.CreateLinkedTokenSource(token);
        _runningJobs[job.Id] = jobCts;

        try
        {
            _logger.LogInfo($"Starting job: {Path.GetFileName(job.InputFilePath)}");

            // Update job status
            job.Status = JobStatus.Converting;
            job.CurrentPhase = ProcessingPhase.Converting;
            job.StartTime = DateTime.Now;
            
            ReportProgress(job, ProcessingPhase.Converting, 0, "Starting conversion...");

            // NOTE: Python worker will handle both audio extraction AND transcription
            // No need to convert here - let Python script do it efficiently
            
            _logger.LogInfo($"Delegating conversion and transcription to Python worker for: {Path.GetFileName(job.InputFilePath)}");

            // Phase 1+2: Python Worker handles BOTH conversion AND transcription (0-100%)
            job.Status = JobStatus.Transcribing;
            job.CurrentPhase = ProcessingPhase.Transcribing;
            ReportProgress(job, ProcessingPhase.Transcribing, 10, "Starting Python worker...");

            var transcriptionProgress = new Progress<JobProgress>(progressInfo =>
            {
                job.Progress = progressInfo.Percent;
                ReportProgress(job, progressInfo.Phase, progressInfo.Percent, progressInfo.Message);
            });

            var result = await _pythonWorker.ProcessAsync(
                job,
                transcriptionProgress,
                jobCts.Token);

            job.Result = result;
            
            _logger.LogInfo($"Transcription completed for: {Path.GetFileName(job.InputFilePath)}");

            // Phase 3: Complete
            job.Status = JobStatus.Completed;
            job.CurrentPhase = ProcessingPhase.Completed;
            job.Progress = 100;
            job.EndTime = DateTime.Now;
            
            ReportProgress(job, ProcessingPhase.Completed, 100, "Completed");

            // Clean up WAV file if configured
            var settings = _settings.LoadSettings();
            if (settings?.Processing?.AutoDeleteWavFiles == true && result.WavFilePath != null)
            {
                try
                {
                    if (File.Exists(result.WavFilePath))
                    {
                        File.Delete(result.WavFilePath);
                        _logger.LogDebug($"Deleted WAV file: {result.WavFilePath}");
                    }
                }
                catch (Exception ex)
                {
                    Utilities.WriteToLog(ex);
                    _logger.LogWarning($"Failed to delete WAV file: {ex.Message}");
                }
            }

            // Raise completion event
            JobCompleted?.Invoke(this, new JobCompletedEventArgs 
            { 
                Job = job,
                IsSuccess = true 
            });

            _logger.LogInfo($"Job completed successfully: {Path.GetFileName(job.InputFilePath)}");
        }
        catch (OperationCanceledException)
        {
            job.Status = JobStatus.Canceled;
            job.CurrentPhase = ProcessingPhase.Failed;
            job.EndTime = DateTime.Now;
            job.ErrorMessage = "Job was cancelled";
            
            ReportProgress(job, ProcessingPhase.Failed, job.Progress, "Cancelled");
            
            JobCompleted?.Invoke(this, new JobCompletedEventArgs 
            { 
                Job = job,
                IsSuccess = false 
            });

            _logger.LogInfo($"Job cancelled: {Path.GetFileName(job.InputFilePath)}");
        }
        catch (Exception ex)
        {
            Utilities.WriteToLog(ex);
            job.Status = JobStatus.Failed;
            job.CurrentPhase = ProcessingPhase.Failed;
            job.EndTime = DateTime.Now;
            job.ErrorMessage = ex.Message;
            
            ReportProgress(job, ProcessingPhase.Failed, job.Progress, $"Error: {ex.Message}");
            
            JobCompleted?.Invoke(this, new JobCompletedEventArgs 
            { 
                Job = job,
                IsSuccess = false 
            });

            _logger.LogError($"Job failed: {Path.GetFileName(job.InputFilePath)} - {ex.Message}", ex);
        }
        finally
        {
            _runningJobs.TryRemove(job.Id, out _);
            jobCts.Dispose();
        }
    }

    private void ReportProgress(TranscriptionJob job, ProcessingPhase phase, int percent, string message)
    {
        try
        {
            var progress = new JobProgress
            {
                Phase = phase,
                Percent = percent,
                Message = message
            };

            ProgressChanged?.Invoke(this, new JobProgressEventArgs
            {
                Job = job,
                Progress = progress
            });
        }
        catch (Exception ex)
        {
            Utilities.WriteToLog(ex);
            _logger?.LogError($"Error in ReportProgress: {ex.Message}", ex);
        }
    }
}
