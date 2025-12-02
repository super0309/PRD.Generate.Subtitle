using VideoSubtitleGenerator.Core.Enums;

namespace VideoSubtitleGenerator.Core.Models;

/// <summary>
/// Represents a transcription job for a single video file
/// </summary>
public class TranscriptionJob
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public string InputFilePath { get; set; } = string.Empty;
    
    public string OutputDirectory { get; set; } = string.Empty;
    
    public JobStatus Status { get; set; } = JobStatus.Pending;
    
    public ProcessingPhase CurrentPhase { get; set; } = ProcessingPhase.Queued;
    
    /// <summary>
    /// Progress percentage (0-100)
    /// </summary>
    public int Progress { get; set; }
    
    public DateTime? StartTime { get; set; }
    
    public DateTime? EndTime { get; set; }
    
    public TimeSpan? EstimatedTimeRemaining { get; set; }
    
    public string? ErrorMessage { get; set; }
    
    public TranscriptionResult? Result { get; set; }
    
    public WhisperSettings Settings { get; set; } = new();
    
    /// <summary>
    /// Get elapsed time since start
    /// </summary>
    public TimeSpan? ElapsedTime => StartTime.HasValue 
        ? (EndTime ?? DateTime.Now) - StartTime.Value 
        : null;
}
