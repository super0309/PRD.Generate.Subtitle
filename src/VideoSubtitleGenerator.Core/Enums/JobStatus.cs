namespace VideoSubtitleGenerator.Core.Enums;

/// <summary>
/// Job processing status
/// </summary>
public enum JobStatus
{
    /// <summary>
    /// Job is waiting in queue
    /// </summary>
    Pending,
    
    /// <summary>
    /// Converting video to WAV audio
    /// </summary>
    Converting,
    
    /// <summary>
    /// Transcribing audio with Whisper AI
    /// </summary>
    Transcribing,
    
    /// <summary>
    /// Job completed successfully
    /// </summary>
    Completed,
    
    /// <summary>
    /// Job failed with error
    /// </summary>
    Failed,
    
    /// <summary>
    /// Job was canceled by user
    /// </summary>
    Canceled
}
