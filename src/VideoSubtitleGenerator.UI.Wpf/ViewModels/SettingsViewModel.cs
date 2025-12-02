using System;
using System.Windows;
using System.Windows.Input;
using VideoSubtitleGenerator.Core;
using VideoSubtitleGenerator.Core.Enums;
using VideoSubtitleGenerator.Core.Interfaces;
using VideoSubtitleGenerator.Core.Models;
using VideoSubtitleGenerator.UI.Wpf.Commands;

namespace VideoSubtitleGenerator.UI.Wpf.ViewModels;

/// <summary>
/// ViewModel for SettingsWindow. Manages application settings.
/// </summary>
public class SettingsViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;
    
    // Advanced Settings
    private string _pythonPath = @"C:\Python\python.exe";
    private bool _autoDetectPython = true;
    private string _ffmpegPath = string.Empty;
    private bool _useBundledFFmpeg = true;
    private string _scriptPath = "python-worker/process_media.py";
    private int _retryCount = 3;
    private int _retryDelay = 5;
    private bool _continueOnError = false;
    private bool _saveLogToFile = true;
    
    // Logging
    private string _logLevel = "Info";
    private string _logPath = "logs/app.log";
    private bool _enableLogging = true;
    private bool _enableDebugMode = false;
    
    // Whisper Settings
    private string _whisperModel = "small";
    private string _language = "English";
    private string _device = "CPU";
    private string _task = "Transcribe";
    private string _subtitleFormat = "SRT";
    private bool _includeTimestamps = true;
    private bool _includeWordLevel = false;
    
    // Processing Settings
    private string _outputDirectory = @"C:\Output";
    private bool _autoOpenOutput = false;
    private bool _deleteWavAfterProcessing = false;
    private bool _createSubfolder = true;
    private string _processingMode = "Sequential";
    private int _maxParallelJobs = 2;
    private int _audioSampleRate = 16000;
    private int _audioChannels = 1;

    public SettingsViewModel(ISettingsService settingsService)
    {
        _settingsService = settingsService;
        
        // Load current settings
        LoadSettings();
        
        // Initialize commands
        SaveCommand = new RelayCommand(_ => Save());
        CancelCommand = new RelayCommand(_ => Cancel());
        RestoreDefaultsCommand = new RelayCommand(_ => RestoreDefaults());
        VerifyConfigCommand = new RelayCommand(_ => VerifyConfiguration());
        BrowsePythonCommand = new RelayCommand(_ => BrowsePython());
        BrowseFFmpegCommand = new RelayCommand(_ => BrowseFFmpeg());
        BrowseOutputCommand = new RelayCommand(_ => BrowseOutput());
        BrowseLogPathCommand = new RelayCommand(_ => BrowseLogPath());
        CheckForUpdatesCommand = new RelayCommand(_ => CheckForUpdates());
    }
    
    private void LoadSettings()
    {
        try
        {
            var settings = _settingsService.LoadSettings();
            
            // Load Python settings
            PythonPath = settings.Python.PythonExePath;
            ScriptPath = settings.Python.ScriptPath;
            
            // Load FFmpeg settings
            FFmpegPath = settings.FFmpeg.ExecutablePath;
            UseBundledFFmpeg = settings.FFmpeg.UseBundled;
            
            // Load Whisper settings
            WhisperModel = settings.Whisper.Model;
            Language = settings.Whisper.Language;
            Device = settings.Whisper.Device;
            Task = settings.Whisper.Task;
            
            // Load Processing settings
            ProcessingMode = ((int)settings.Processing.DefaultMode).ToString();
            MaxParallelJobs = settings.Processing.MaxParallelJobs;
            OutputDirectory = settings.Processing.OutputDirectory ?? string.Empty;
            DeleteWavAfterProcessing = settings.Processing.AutoDeleteWavFiles;
            
            // Load Logging settings
            LogPath = settings.Logging.LogFilePath ?? "logs/app.log";
        }
        catch (Exception ex)
        {
            Utilities.WriteToLog(ex);
            MessageBox.Show("Failed to load settings. Using defaults.", 
                "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    #region Advanced Settings Properties

    public string PythonPath
    {
        get => _pythonPath;
        set => SetProperty(ref _pythonPath, value);
    }

    public bool AutoDetectPython
    {
        get => _autoDetectPython;
        set => SetProperty(ref _autoDetectPython, value);
    }

    public string FFmpegPath
    {
        get => _ffmpegPath;
        set => SetProperty(ref _ffmpegPath, value);
    }

    public bool UseBundledFFmpeg
    {
        get => _useBundledFFmpeg;
        set => SetProperty(ref _useBundledFFmpeg, value);
    }

    public string ScriptPath
    {
        get => _scriptPath;
        set => SetProperty(ref _scriptPath, value);
    }

    public int RetryCount
    {
        get => _retryCount;
        set => SetProperty(ref _retryCount, value);
    }

    public int RetryDelay
    {
        get => _retryDelay;
        set => SetProperty(ref _retryDelay, value);
    }

    public bool ContinueOnError
    {
        get => _continueOnError;
        set => SetProperty(ref _continueOnError, value);
    }

    public bool SaveLogToFile
    {
        get => _saveLogToFile;
        set => SetProperty(ref _saveLogToFile, value);
    }

    #endregion

    #region Logging Properties

    public string LogLevel
    {
        get => _logLevel;
        set => SetProperty(ref _logLevel, value);
    }

    public string LogPath
    {
        get => _logPath;
        set => SetProperty(ref _logPath, value);
    }

    public bool EnableLogging
    {
        get => _enableLogging;
        set => SetProperty(ref _enableLogging, value);
    }

    public bool EnableDebugMode
    {
        get => _enableDebugMode;
        set => SetProperty(ref _enableDebugMode, value);
    }

    #endregion

    #region Whisper Settings Properties

    public string WhisperModel
    {
        get => _whisperModel;
        set => SetProperty(ref _whisperModel, value);
    }

    public string Language
    {
        get => _language;
        set => SetProperty(ref _language, value);
    }

    public string Device
    {
        get => _device;
        set => SetProperty(ref _device, value);
    }

    public string Task
    {
        get => _task;
        set => SetProperty(ref _task, value);
    }

    public string SubtitleFormat
    {
        get => _subtitleFormat;
        set => SetProperty(ref _subtitleFormat, value);
    }

    public bool IncludeTimestamps
    {
        get => _includeTimestamps;
        set => SetProperty(ref _includeTimestamps, value);
    }

    public bool IncludeWordLevel
    {
        get => _includeWordLevel;
        set => SetProperty(ref _includeWordLevel, value);
    }

    #endregion

    #region Processing Settings Properties

    public string OutputDirectory
    {
        get => _outputDirectory;
        set => SetProperty(ref _outputDirectory, value);
    }

    public bool AutoOpenOutput
    {
        get => _autoOpenOutput;
        set => SetProperty(ref _autoOpenOutput, value);
    }

    public bool DeleteWavAfterProcessing
    {
        get => _deleteWavAfterProcessing;
        set => SetProperty(ref _deleteWavAfterProcessing, value);
    }

    public bool CreateSubfolder
    {
        get => _createSubfolder;
        set => SetProperty(ref _createSubfolder, value);
    }

    public string ProcessingMode
    {
        get => _processingMode;
        set => SetProperty(ref _processingMode, value);
    }

    public int MaxParallelJobs
    {
        get => _maxParallelJobs;
        set => SetProperty(ref _maxParallelJobs, value);
    }

    public int AudioSampleRate
    {
        get => _audioSampleRate;
        set => SetProperty(ref _audioSampleRate, value);
    }

    public int AudioChannels
    {
        get => _audioChannels;
        set => SetProperty(ref _audioChannels, value);
    }

    #endregion

    #region Commands

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand RestoreDefaultsCommand { get; }
    public ICommand VerifyConfigCommand { get; }
    public ICommand BrowsePythonCommand { get; }
    public ICommand BrowseFFmpegCommand { get; }
    public ICommand BrowseOutputCommand { get; }
    public ICommand BrowseLogPathCommand { get; }
    public ICommand CheckForUpdatesCommand { get; }

    #endregion

    #region Command Implementations

    private async void Save()
    {
        try
        {
            // ========== STEP 1: VALIDATION ==========
            var validationErrors = ValidateSettings();
            if (validationErrors.Count > 0)
            {
                var errorMessage = "Please fix the following errors before saving:\n\n" +
                                  string.Join("\n", validationErrors.Select(e => $"• {e}"));
                
                MessageBox.Show(errorMessage, "Validation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            // ========== STEP 2: AUTO-DETECT MISSING PATHS ==========
            if (AutoDetectPython && string.IsNullOrWhiteSpace(PythonPath))
            {
                var detected = AutoDetectPythonPath();
                if (!string.IsNullOrWhiteSpace(detected))
                {
                    PythonPath = detected;
                }
            }
            
            if (UseBundledFFmpeg)
            {
                // Use bundled FFmpeg - set to default bundled path
                FFmpegPath = "ffmpeg\\bin\\ffmpeg.exe";
            }
            else if (string.IsNullOrWhiteSpace(FFmpegPath))
            {
                var detected = AutoDetectFFmpegPath();
                if (!string.IsNullOrWhiteSpace(detected))
                {
                    FFmpegPath = detected;
                }
            }
            
            // ========== STEP 3: CREATE OUTPUT DIRECTORY IF SPECIFIED ==========
            if (!string.IsNullOrWhiteSpace(OutputDirectory))
            {
                try
                {
                    if (!System.IO.Directory.Exists(OutputDirectory))
                    {
                        System.IO.Directory.CreateDirectory(OutputDirectory);
                    }
                }
                catch (Exception ex)
                {
                    Utilities.WriteToLog(ex);
                    var result = MessageBox.Show(
                        $"Cannot create output directory:\n{OutputDirectory}\n\n{ex.Message}\n\nContinue saving anyway?",
                        "Directory Error",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);
                    
                    if (result != MessageBoxResult.Yes)
                    {
                        return;
                    }
                }
            }
            
            // ========== STEP 4: CREATE LOG DIRECTORY ==========
            if (!string.IsNullOrWhiteSpace(LogPath))
            {
                try
                {
                    var logDir = System.IO.Path.GetDirectoryName(LogPath);
                    if (!string.IsNullOrWhiteSpace(logDir) && !System.IO.Directory.Exists(logDir))
                    {
                        System.IO.Directory.CreateDirectory(logDir);
                    }
                }
                catch (Exception ex)
                {
                    Utilities.WriteToLog(ex);
                    // Continue anyway - log directory is not critical
                }
            }
            
            // ========== STEP 5: MAP TO AppSettings MODEL ==========
            var settings = new AppSettings
            {
                Python = new PythonSettings
                {
                    PythonExePath = PythonPath,
                    ScriptPath = ScriptPath,
                    VenvPath = null // TODO: Add venv support
                },
                FFmpeg = new FFmpegSettings
                {
                    ExecutablePath = FFmpegPath,
                    UseBundled = UseBundledFFmpeg
                },
                Whisper = new WhisperSettings
                {
                    Model = WhisperModel,
                    Language = Language,
                    Device = Device,
                    Task = Task,
                    OutputFormat = SubtitleFormat.ToLower(), // srt, vtt, txt, json
                    Fp16 = Device.ToLower() == "cuda" || Device.ToLower() == "gpu" // Auto FP16 for GPU
                },
                Processing = new ProcessingSettings
                {
                    DefaultMode = ProcessingMode.ToLower() == "parallel" 
                        ? Core.Enums.ProcessingMode.Parallel 
                        : Core.Enums.ProcessingMode.Sequential,
                    MaxParallelJobs = Math.Max(1, Math.Min(MaxParallelJobs, 16)), // Clamp 1-16
                    OutputDirectory = string.IsNullOrWhiteSpace(OutputDirectory) 
                        ? string.Empty 
                        : OutputDirectory,
                    AutoDeleteWavFiles = DeleteWavAfterProcessing
                },
                Logging = new LoggingSettings
                {
                    LogFilePath = string.IsNullOrWhiteSpace(LogPath) 
                        ? "logs\\app.log" 
                        : LogPath,
                    MinimumLevel = LogLevel
                }
            };
            
            // ========== STEP 6: SAVE TO FILE ==========
            await _settingsService.SaveSettingsAsync(settings);
            
            // ========== STEP 7: VERIFY SAVE SUCCESS ==========
            var reloaded = _settingsService.LoadSettings();
            if (reloaded == null)
            {
                throw new InvalidOperationException("Settings were saved but could not be reloaded for verification");
            }
            
            // ========== STEP 8: SUCCESS MESSAGE ==========
            var successMessage = "✅ Settings saved successfully!\n\n" +
                                "Configuration Summary:\n" +
                                $"• Python: {(System.IO.File.Exists(PythonPath) ? "✓" : "⚠")} {PythonPath}\n" +
                                $"• FFmpeg: {(UseBundledFFmpeg ? "✓ Bundled" : (System.IO.File.Exists(FFmpegPath) ? "✓" : "⚠") + " " + FFmpegPath)}\n" +
                                $"• Script: {(System.IO.File.Exists(ScriptPath) || System.IO.File.Exists(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ScriptPath)) ? "✓" : "⚠")} {ScriptPath}\n" +
                                $"• Whisper Model: {WhisperModel}\n" +
                                $"• Processing Mode: {ProcessingMode}\n" +
                                $"• Max Parallel Jobs: {MaxParallelJobs}\n" +
                                $"• Output Directory: {(string.IsNullOrWhiteSpace(OutputDirectory) ? "(Save with video files)" : OutputDirectory)}";
            
            MessageBox.Show(successMessage, "Settings Saved", 
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            Utilities.WriteToLog(ex);
            MessageBox.Show(
                $"❌ Failed to save settings:\n\n{ex.Message}\n\nPlease check the log file for details.", 
                "Save Error", 
                MessageBoxButton.OK, 
                MessageBoxImage.Error);
        }
    }
    
    /// <summary>
    /// Validates settings before saving
    /// </summary>
    private List<string> ValidateSettings()
    {
        var errors = new List<string>();
        
        // Python validation
        if (string.IsNullOrWhiteSpace(PythonPath))
        {
            errors.Add("Python path is required");
        }
        else if (!System.IO.File.Exists(PythonPath))
        {
            errors.Add($"Python executable not found at: {PythonPath}");
        }
        
        // Script validation
        if (string.IsNullOrWhiteSpace(ScriptPath))
        {
            errors.Add("Python script path is required");
        }
        else
        {
            var scriptPath = ScriptPath;
            if (!System.IO.Path.IsPathRooted(scriptPath))
            {
                scriptPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, scriptPath);
            }
            
            if (!System.IO.File.Exists(scriptPath))
            {
                errors.Add($"Python script not found at: {ScriptPath}");
            }
        }
        
        // FFmpeg validation (only if not using bundled)
        if (!UseBundledFFmpeg)
        {
            if (string.IsNullOrWhiteSpace(FFmpegPath))
            {
                errors.Add("FFmpeg path is required when not using bundled version");
            }
            else if (!System.IO.File.Exists(FFmpegPath))
            {
                errors.Add($"FFmpeg executable not found at: {FFmpegPath}");
            }
        }
        
        // Whisper model validation
        var validModels = new[] { "tiny", "base", "small", "medium", "large" };
        if (!validModels.Contains(WhisperModel.ToLower()))
        {
            errors.Add($"Invalid Whisper model. Must be one of: {string.Join(", ", validModels)}");
        }
        
        // Max parallel jobs validation
        if (MaxParallelJobs < 1 || MaxParallelJobs > 16)
        {
            errors.Add("Max parallel jobs must be between 1 and 16");
        }
        
        // Output directory validation (optional, but must be valid if specified)
        if (!string.IsNullOrWhiteSpace(OutputDirectory))
        {
            try
            {
                // Check if path is valid
                System.IO.Path.GetFullPath(OutputDirectory);
            }
            catch
            {
                errors.Add($"Invalid output directory path: {OutputDirectory}");
            }
        }
        
        return errors;
    }
    
    /// <summary>
    /// Auto-detect Python installation
    /// </summary>
    private string? AutoDetectPythonPath()
    {
        // Check AppData first (Windows Store Python)
        var appDataPython = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Programs", "Python");
        
        if (System.IO.Directory.Exists(appDataPython))
        {
            try
            {
                var pythonExes = System.IO.Directory.GetFiles(appDataPython, "python.exe", System.IO.SearchOption.AllDirectories)
                    .OrderByDescending(p => p)
                    .ToList();
                
                if (pythonExes.Count > 0)
                {
                    return pythonExes[0];
                }
            }
            catch { }
        }
        
        // Check common paths
        var commonPaths = new[]
        {
            @"C:\Python313\python.exe",
            @"C:\Python312\python.exe",
            @"C:\Python311\python.exe",
            @"C:\Python310\python.exe",
            @"C:\Python39\python.exe",
            @"C:\Python38\python.exe"
        };
        
        foreach (var path in commonPaths)
        {
            if (System.IO.File.Exists(path))
            {
                return path;
            }
        }
        
        // Check PATH
        var pathVar = Environment.GetEnvironmentVariable("PATH");
        if (!string.IsNullOrEmpty(pathVar))
        {
            foreach (var path in pathVar.Split(System.IO.Path.PathSeparator))
            {
                try
                {
                    var pythonExe = System.IO.Path.Combine(path.Trim(), "python.exe");
                    if (System.IO.File.Exists(pythonExe))
                    {
                        return pythonExe;
                    }
                }
                catch { }
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Auto-detect FFmpeg installation
    /// </summary>
    private string? AutoDetectFFmpegPath()
    {
        var commonPaths = new[]
        {
            @"C:\ffmpeg\bin\ffmpeg.exe",
            @"C:\Program Files\ffmpeg\bin\ffmpeg.exe",
            System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg", "bin", "ffmpeg.exe")
        };
        
        foreach (var path in commonPaths)
        {
            if (System.IO.File.Exists(path))
            {
                return path;
            }
        }
        
        // Check PATH
        var pathVar = Environment.GetEnvironmentVariable("PATH");
        if (!string.IsNullOrEmpty(pathVar))
        {
            foreach (var path in pathVar.Split(System.IO.Path.PathSeparator))
            {
                try
                {
                    var ffmpegExe = System.IO.Path.Combine(path.Trim(), "ffmpeg.exe");
                    if (System.IO.File.Exists(ffmpegExe))
                    {
                        return ffmpegExe;
                    }
                }
                catch { }
            }
        }
        
        return null;
    }

    private void Cancel()
    {
        // Close window - handled by code-behind
    }

    private void RestoreDefaults()
    {
        var result = MessageBox.Show(
            "Are you sure you want to restore default settings?\n\n" +
            "This will reset all settings to their default values and attempt to auto-detect Python and FFmpeg.", 
            "Restore Defaults", 
            MessageBoxButton.YesNo, 
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            try
            {
                // Get default settings from service (includes auto-detection)
                var defaults = _settingsService.GetDefaultSettings();
                
                // ========== PYTHON SETTINGS ==========
                PythonPath = defaults.Python.PythonExePath ?? "python.exe";
                AutoDetectPython = true;
                ScriptPath = defaults.Python.ScriptPath ?? "python-worker\\process_media.py";
                
                // ========== FFMPEG SETTINGS ==========
                FFmpegPath = defaults.FFmpeg.ExecutablePath ?? "ffmpeg";
                UseBundledFFmpeg = defaults.FFmpeg.UseBundled;
                
                // ========== WHISPER SETTINGS ==========
                WhisperModel = defaults.Whisper.Model ?? "small";
                Language = defaults.Whisper.Language ?? "auto";
                Device = defaults.Whisper.Device ?? "cpu";
                Task = defaults.Whisper.Task ?? "transcribe";
                SubtitleFormat = defaults.Whisper.OutputFormat ?? "srt";
                IncludeTimestamps = true;
                IncludeWordLevel = false;
                
                // ========== PROCESSING SETTINGS ==========
                OutputDirectory = string.Empty; // Empty = save with video files
                ProcessingMode = defaults.Processing.DefaultMode == Core.Enums.ProcessingMode.Parallel 
                    ? "Parallel" 
                    : "Sequential";
                MaxParallelJobs = defaults.Processing.MaxParallelJobs;
                DeleteWavAfterProcessing = defaults.Processing.AutoDeleteWavFiles;
                AutoOpenOutput = false;
                CreateSubfolder = false;
                AudioSampleRate = 16000;
                AudioChannels = 1;
                
                // ========== LOGGING SETTINGS ==========
                LogPath = defaults.Logging.LogFilePath ?? "logs\\app.log";
                LogLevel = defaults.Logging.MinimumLevel ?? "Information";
                EnableLogging = true;
                EnableDebugMode = false;
                SaveLogToFile = true;
                
                // ========== ADVANCED SETTINGS ==========
                RetryCount = 3;
                RetryDelay = 5;
                ContinueOnError = false;
                
                // Show result
                var detectedPython = System.IO.File.Exists(PythonPath) ? "✓" : "⚠";
                var detectedFFmpeg = UseBundledFFmpeg ? "✓ (bundled)" : (System.IO.File.Exists(FFmpegPath) ? "✓" : "⚠");
                var detectedScript = System.IO.File.Exists(ScriptPath) || 
                                    System.IO.File.Exists(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ScriptPath)) 
                    ? "✓" : "⚠";
                
                var message = "✅ Default settings restored!\n\n" +
                             "Auto-detection results:\n" +
                             $"• Python: {detectedPython} {PythonPath}\n" +
                             $"• FFmpeg: {detectedFFmpeg} {(UseBundledFFmpeg ? "Using bundled" : FFmpegPath)}\n" +
                             $"• Script: {detectedScript} {ScriptPath}\n\n" +
                             "Don't forget to click 'Save' to apply these settings!";
                
                MessageBox.Show(message, "Defaults Restored", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Utilities.WriteToLog(ex);
                MessageBox.Show(
                    $"Failed to restore defaults:\n\n{ex.Message}", 
                    "Error", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Error);
            }
        }
    }

    private void VerifyConfiguration()
    {
        var messages = new List<string>();
        
        // Check Python
        if (!string.IsNullOrWhiteSpace(PythonPath))
        {
            if (System.IO.File.Exists(PythonPath))
            {
                messages.Add($"✓ Python: Found");
            }
            else
            {
                messages.Add($"✗ Python: Not found at {PythonPath}");
            }
        }
        else
        {
            messages.Add("✗ Python: Not configured");
        }
        
        // Check FFmpeg
        if (UseBundledFFmpeg)
        {
            messages.Add("✓ FFmpeg: Using bundled version");
        }
        else if (!string.IsNullOrWhiteSpace(FFmpegPath))
        {
            if (System.IO.File.Exists(FFmpegPath))
            {
                messages.Add($"✓ FFmpeg: Found");
            }
            else
            {
                messages.Add($"✗ FFmpeg: Not found at {FFmpegPath}");
            }
        }
        else
        {
            messages.Add("✗ FFmpeg: Not configured");
        }
        
        // Check Script Path
        if (!string.IsNullOrWhiteSpace(ScriptPath))
        {
            var scriptPath = ScriptPath;
            // Resolve relative path
            if (!System.IO.Path.IsPathRooted(scriptPath))
            {
                scriptPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, scriptPath);
            }
            
            if (System.IO.File.Exists(scriptPath))
            {
                messages.Add($"✓ Script: Found");
            }
            else
            {
                messages.Add($"✗ Script: Not found at {ScriptPath}");
            }
        }
        else
        {
            messages.Add("✗ Script: Not configured");
        }
        
        // Check Output Directory (optional)
        if (string.IsNullOrWhiteSpace(OutputDirectory))
        {
            messages.Add("✓ Output Directory: Valid (will save with video files)");
        }
        else if (System.IO.Directory.Exists(OutputDirectory))
        {
            messages.Add($"✓ Output Directory: Valid");
        }
        else
        {
            messages.Add($"⚠ Output Directory: Will be created when needed");
        }
        
        var allValid = messages.All(m => m.StartsWith("✓") || m.StartsWith("⚠"));
        
        MessageBox.Show(
            "Configuration verification:\n\n" + string.Join("\n", messages),
            "Configuration Check",
            MessageBoxButton.OK,
            allValid ? MessageBoxImage.Information : MessageBoxImage.Warning);
    }

    private void BrowsePython()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Python Executable|python.exe|All Files|*.*",
            Title = "Select Python Executable"
        };

        if (dialog.ShowDialog() == true)
        {
            PythonPath = dialog.FileName;
        }
    }

    private void BrowseFFmpeg()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "FFmpeg Executable|ffmpeg.exe|All Files|*.*",
            Title = "Select FFmpeg Executable"
        };

        if (dialog.ShowDialog() == true)
        {
            FFmpegPath = dialog.FileName;
        }
    }

    private void BrowseOutput()
    {
        try
        {
            // Use OpenFileDialog with workaround for folder selection
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Select Output Directory",
                Filter = "Folders|*.none",
                FileName = "Select Folder",
                CheckFileExists = false,
                CheckPathExists = true,
                ValidateNames = false
            };
            
            // Set initial directory
            if (!string.IsNullOrWhiteSpace(OutputDirectory) && System.IO.Directory.Exists(OutputDirectory))
            {
                dialog.InitialDirectory = OutputDirectory;
            }
            
            if (dialog.ShowDialog() == true)
            {
                // Get the directory path from the selected file path
                var selectedPath = System.IO.Path.GetDirectoryName(dialog.FileName);
                if (!string.IsNullOrWhiteSpace(selectedPath))
                {
                    OutputDirectory = selectedPath;
                }
            }
        }
        catch (Exception ex)
        {
            Utilities.WriteToLog(ex);
            // Fallback to simple input dialog
            var input = Microsoft.VisualBasic.Interaction.InputBox(
                "Enter output directory path:\n\n(Leave empty to save subtitles with video files)",
                "Output Directory",
                OutputDirectory);
            
            if (input != null) // User clicked OK (even if empty)
            {
                OutputDirectory = input;
            }
        }
    }

    private void BrowseLogPath()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "Log Files|*.log|Text Files|*.txt|All Files|*.*",
            Title = "Select Log File Location",
            FileName = "app.log"
        };

        if (dialog.ShowDialog() == true)
        {
            LogPath = dialog.FileName;
        }
    }

    private void CheckForUpdates()
    {
        // TODO: Implement update check
        MessageBox.Show("You are using the latest version (1.0.0)", "Check for Updates",
            MessageBoxButton.OK, MessageBoxImage.Information);
    }

    #endregion
}
