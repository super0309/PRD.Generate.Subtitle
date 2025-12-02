using System;
using System.Threading;
using System.Threading.Tasks;

namespace VideoSubtitleGenerator.Core.Interfaces;

/// <summary>
/// Service for FFmpeg video/audio conversion operations
/// </summary>
public interface IFFmpegService
{
    /// <summary>
    /// Converts video file to WAV audio format (16kHz, mono)
    /// </summary>
    /// <param name="videoPath">Path to input video file</param>
    /// <param name="outputPath">Path for output WAV file (optional)</param>
    /// <param name="progress">Progress reporter (0-100)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Path to generated WAV file</returns>
    Task<string> ConvertToWavAsync(
        string videoPath,
        string outputPath,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets video duration
    /// </summary>
    Task<TimeSpan> GetDurationAsync(string videoPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets media information (format, codec, etc.)
    /// </summary>
    Task<string> GetMediaInfoAsync(string videoPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates FFmpeg installation
    /// </summary>
    Task<bool> ValidateInstallationAsync();
}
