namespace VideoSubtitleGenerator.Core.Enums;

/// <summary>
/// Processing mode for batch jobs
/// </summary>
public enum ProcessingMode
{
    /// <summary>
    /// Process jobs one at a time (sequential)
    /// </summary>
    Sequential,
    
    /// <summary>
    /// Process multiple jobs simultaneously (parallel)
    /// </summary>
    Parallel
}
