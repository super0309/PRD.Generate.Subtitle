namespace VideoSubtitleGenerator.Core.Models;

/// <summary>
/// Whisper AI configuration settings
/// </summary>
public class WhisperSettings
{
    /// <summary>
    /// Whisper model size (tiny, base, small, medium, large)
    /// </summary>
    public string Model { get; set; } = "small";
    
    /// <summary>
    /// Target language (English, Vietnamese, etc.)
    /// </summary>
    public string Language { get; set; } = "English";
    
    /// <summary>
    /// Processing device (cpu or cuda)
    /// </summary>
    public string Device { get; set; } = "cpu";
    
    /// <summary>
    /// Enable FP16 precision (GPU only)
    /// </summary>
    public bool Fp16 { get; set; } = false;
    
    /// <summary>
    /// Task type (transcribe or translate)
    /// </summary>
    public string Task { get; set; } = "transcribe";
    
    /// <summary>
    /// Output subtitle format (srt, vtt, txt, json)
    /// </summary>
    public string OutputFormat { get; set; } = "srt";
}
