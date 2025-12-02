using VideoSubtitleGenerator.Core.Models;

namespace VideoSubtitleGenerator.Core.Interfaces;

/// <summary>
/// Service for executing Python worker to process video files
/// </summary>
public interface IPythonWorkerService
{
    /// <summary>
    /// Process a transcription job asynchronously
    /// </summary>
    Task<TranscriptionResult> ProcessAsync(
        TranscriptionJob job,
        IProgress<JobProgress>? progress = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Validate that Python environment is properly configured
    /// </summary>
    Task<bool> ValidateEnvironmentAsync();
    
    /// <summary>
    /// Get Python version string
    /// </summary>
    string GetPythonVersion();
}
