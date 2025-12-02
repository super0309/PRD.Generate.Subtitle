using VideoSubtitleGenerator.Core.Enums;
using VideoSubtitleGenerator.Core.Models;

namespace VideoSubtitleGenerator.Core.Interfaces;

/// <summary>
/// Service for orchestrating batch job processing
/// </summary>
public interface IJobOrchestrator
{
    /// <summary>
    /// Start processing jobs
    /// </summary>
    Task StartProcessingAsync(
        ProcessingMode mode,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Pause current processing
    /// </summary>
    Task PauseAsync();
    
    /// <summary>
    /// Resume paused processing
    /// </summary>
    Task ResumeAsync();
    
    /// <summary>
    /// Cancel all processing
    /// </summary>
    Task CancelAsync();
    
    /// <summary>
    /// Whether processing is currently running
    /// </summary>
    bool IsRunning { get; }
    
    /// <summary>
    /// Number of active worker tasks
    /// </summary>
    int ActiveWorkers { get; }
    
    /// <summary>
    /// Event fired when progress changes
    /// </summary>
    event EventHandler<JobProgressEventArgs>? ProgressChanged;
    
    /// <summary>
    /// Event fired when a job completes
    /// </summary>
    event EventHandler<JobCompletedEventArgs>? JobCompleted;
}

public class JobProgressEventArgs : EventArgs
{
    public TranscriptionJob Job { get; set; } = null!;
    public JobProgress Progress { get; set; } = null!;
}

public class JobCompletedEventArgs : EventArgs
{
    public TranscriptionJob Job { get; set; } = null!;
    public bool IsSuccess { get; set; }
}
