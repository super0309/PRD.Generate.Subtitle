using VideoSubtitleGenerator.Core.Models;

namespace VideoSubtitleGenerator.Core.Interfaces;

/// <summary>
/// Service for loading and saving application settings
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// Load settings from storage
    /// </summary>
    AppSettings LoadSettings();
    
    /// <summary>
    /// Save settings to storage
    /// </summary>
    Task SaveSettingsAsync(AppSettings settings);
    
    /// <summary>
    /// Get default settings
    /// </summary>
    AppSettings GetDefaultSettings();
}
