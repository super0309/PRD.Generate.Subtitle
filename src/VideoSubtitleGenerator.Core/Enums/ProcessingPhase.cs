namespace VideoSubtitleGenerator.Core.Enums;

/// <summary>
/// Current processing phase of a job
/// </summary>
public enum ProcessingPhase
{
    /// <summary>
    /// Job is queued and waiting
    /// </summary>
    Queued,
    
    /// <summary>
    /// FFmpeg is converting video to audio
    /// </summary>
    Converting,
    
    /// <summary>
    /// Whisper AI is transcribing audio
    /// </summary>
    Transcribing,
    
    /// <summary>
    /// Finalizing output files
    /// </summary>
    Finalizing,
    
    /// <summary>
    /// Job completed
    /// </summary>
    Completed,
    
    /// <summary>
    /// Job failed
    /// </summary>
    Failed
}
