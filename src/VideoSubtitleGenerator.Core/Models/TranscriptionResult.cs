namespace VideoSubtitleGenerator.Core.Models;

/// <summary>
/// Result of transcription process
/// </summary>
public class TranscriptionResult
{
    public bool IsSuccess { get; set; }
    
    public string? WavFilePath { get; set; }
    
    public string? SubtitleFilePath { get; set; }
    
    public TimeSpan Duration { get; set; }
    
    public string? ErrorMessage { get; set; }
    
    public Dictionary<string, string> Metadata { get; set; } = new();
}
