using VideoSubtitleGenerator.Core.Enums;

namespace VideoSubtitleGenerator.Core.Models;

/// <summary>
/// Progress information for a job
/// </summary>
public class JobProgress
{
    public ProcessingPhase Phase { get; set; }
    
    public int Percent { get; set; }
    
    public string Message { get; set; } = string.Empty;
}
