using VideoSubtitleGenerator.Core.Models;

namespace VideoSubtitleGenerator.Core.Interfaces;

/// <summary>
/// Service for managing job queue
/// </summary>
public interface IJobQueueService
{
    /// <summary>
    /// Add jobs to the queue
    /// </summary>
    void EnqueueJobs(IEnumerable<TranscriptionJob> jobs);
    
    /// <summary>
    /// Get next pending job from queue
    /// </summary>
    TranscriptionJob? DequeueJob();
    
    /// <summary>
    /// Get all jobs in the queue
    /// </summary>
    IReadOnlyList<TranscriptionJob> GetAllJobs();
    
    /// <summary>
    /// Get job by ID
    /// </summary>
    TranscriptionJob? GetJobById(Guid id);
    
    /// <summary>
    /// Update an existing job
    /// </summary>
    void UpdateJob(TranscriptionJob job);
    
    /// <summary>
    /// Remove completed jobs from queue
    /// </summary>
    void ClearCompleted();
    
    /// <summary>
    /// Cancel all pending jobs
    /// </summary>
    void CancelAll();
    
    /// <summary>
    /// Event fired when job status changes
    /// </summary>
    event EventHandler<JobEventArgs>? JobStatusChanged;
}

public class JobEventArgs : EventArgs
{
    public TranscriptionJob Job { get; set; } = null!;
}
