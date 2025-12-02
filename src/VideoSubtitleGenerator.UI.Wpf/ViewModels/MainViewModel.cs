using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using VideoSubtitleGenerator.Core;
using VideoSubtitleGenerator.Core.Enums;
using VideoSubtitleGenerator.Core.Interfaces;
using VideoSubtitleGenerator.Core.Models;
using VideoSubtitleGenerator.UI.Wpf.Commands;

namespace VideoSubtitleGenerator.UI.Wpf.ViewModels;

/// <summary>
/// Main ViewModel for the MainWindow. Manages the job queue and processing operations.
/// </summary>
public class MainViewModel : ViewModelBase
{
    private readonly ISettingsService? _settingsService;
    private readonly ILogService? _logService;
    private readonly IPythonWorkerService? _pythonWorkerService;
    private readonly IJobQueueService? _jobQueueService;
    private readonly IJobOrchestrator? _jobOrchestrator;
    
    private int _processingCount;
    private int _completedCount;
    private int _failedCount;
    private TimeSpan _totalElapsedTime;
    private TimeSpan _estimatedTimeRemaining;
    private string _logText = string.Empty;
    
    // Configuration validation
    private bool _isConfigurationValid;
    private string _configurationError = string.Empty;

    public MainViewModel(
        ISettingsService settingsService,
        ILogService logService,
        IPythonWorkerService? pythonWorkerService = null,
        IJobQueueService? jobQueueService = null,
        IJobOrchestrator? jobOrchestrator = null)
    {
        _settingsService = settingsService;
        _logService = logService;
        _pythonWorkerService = pythonWorkerService;
        _jobQueueService = jobQueueService;
        _jobOrchestrator = jobOrchestrator;
        
        // Subscribe to orchestrator events
        if (_jobOrchestrator != null)
        {
            _jobOrchestrator.ProgressChanged += OnJobProgressChanged;
            _jobOrchestrator.JobCompleted += OnJobCompleted;
        }
        
        // Initialize collections
        Jobs = new ObservableCollection<TranscriptionJobViewModel>();
        LogEntries = new ObservableCollection<string>();

        // Initialize commands
        AddFilesCommand = new RelayCommand(_ => AddFiles());
        RemoveSelectedCommand = new RelayCommand(_ => RemoveSelected(), _ => HasSelectedJobs);
        StartProcessingCommand = new AsyncRelayCommand(StartProcessingAsync, () => CanStartProcessing);
        PauseProcessingCommand = new RelayCommand(_ => PauseProcessing(), _ => IsProcessing);
        StopProcessingCommand = new RelayCommand(_ => StopProcessing(), _ => IsProcessing);
        ClearCompletedCommand = new RelayCommand(_ => ClearCompleted(), _ => HasCompletedJobs);
        OpenOutputFolderCommand = new RelayCommand(_ => OpenOutputFolder());
        OpenSettingsCommand = new RelayCommand(_ => OpenSettings());
        ViewJobDetailsCommand = new RelayCommand<TranscriptionJobViewModel>(job => ViewJobDetails(job));
        ClearLogCommand = new RelayCommand(_ => ClearLog());
        SaveLogCommand = new RelayCommand(_ => SaveLog());
        ValidateConfigurationCommand = new RelayCommand(_ => ValidateConfigurationManual());

        // Test data for development
        AddTestData();
        
        // Initialize configuration on startup
        InitializeConfiguration();
    }

    #region Properties

    /// <summary>
    /// Gets the collection of transcription jobs.
    /// </summary>
    public ObservableCollection<TranscriptionJobViewModel> Jobs { get; }

    /// <summary>
    /// Gets the collection of log entries.
    /// </summary>
    public ObservableCollection<string> LogEntries { get; }

    /// <summary>
    /// Gets or sets the number of jobs currently processing.
    /// </summary>
    public int ProcessingCount
    {
        get => _processingCount;
        set => SetProperty(ref _processingCount, value);
    }

    /// <summary>
    /// Gets or sets the number of completed jobs.
    /// </summary>
    public int CompletedCount
    {
        get => _completedCount;
        set => SetProperty(ref _completedCount, value);
    }

    /// <summary>
    /// Gets or sets the number of failed jobs.
    /// </summary>
    public int FailedCount
    {
        get => _failedCount;
        set => SetProperty(ref _failedCount, value);
    }

    /// <summary>
    /// Gets the total number of jobs.
    /// </summary>
    public int TotalJobs => Jobs.Count;

    /// <summary>
    /// Gets or sets the total elapsed time.
    /// </summary>
    public TimeSpan TotalElapsedTime
    {
        get => _totalElapsedTime;
        set
        {
            if (SetProperty(ref _totalElapsedTime, value))
            {
                OnPropertyChanged(nameof(TotalElapsedTimeText));
            }
        }
    }

    /// <summary>
    /// Gets formatted total elapsed time text.
    /// </summary>
    public string TotalElapsedTimeText
    {
        get
        {
            var ts = TotalElapsedTime;
            return ts.TotalHours >= 1 
                ? ts.ToString(@"hh\:mm\:ss") 
                : ts.ToString(@"mm\:ss");
        }
    }

    /// <summary>
    /// Gets or sets the estimated time remaining.
    /// </summary>
    public TimeSpan EstimatedTimeRemaining
    {
        get => _estimatedTimeRemaining;
        set
        {
            if (SetProperty(ref _estimatedTimeRemaining, value))
            {
                OnPropertyChanged(nameof(EstimatedTimeRemainingText));
            }
        }
    }

    /// <summary>
    /// Gets formatted estimated time remaining text.
    /// </summary>
    public string EstimatedTimeRemainingText
    {
        get
        {
            var ts = EstimatedTimeRemaining;
            return ts.TotalHours >= 1 
                ? ts.ToString(@"hh\:mm\:ss") 
                : ts.ToString(@"mm\:ss");
        }
    }

    /// <summary>
    /// Gets or sets the log text.
    /// </summary>
    public string LogText
    {
        get => _logText;
        set => SetProperty(ref _logText, value);
    }

    /// <summary>
    /// Gets the overall progress percentage.
    /// </summary>
    public int OverallProgress
    {
        get
        {
            if (Jobs.Count == 0) return 0;
            return (int)Math.Round(Jobs.Average(j => j.Progress));
        }
    }

    /// <summary>
    /// Gets whether processing is currently active.
    /// </summary>
    public bool IsProcessing => _jobOrchestrator?.IsRunning == true || ProcessingCount > 0;

    /// <summary>
    /// Gets whether there are any selected jobs.
    /// </summary>
    public bool HasSelectedJobs => Jobs.Any(j => j.IsSelected);

    /// <summary>
    /// Gets whether there are any completed jobs.
    /// </summary>
    public bool HasCompletedJobs => Jobs.Any(j => j.IsCompleted);

    /// <summary>
    /// Gets whether processing can be started.
    /// </summary>
    public bool CanStartProcessing
    {
        get
        {
            // Must have valid configuration
            if (!_isConfigurationValid)
            {
                return false;
            }
            
            var hasPending = Jobs.Any(j => j.Status == JobStatus.Pending);
            var result = hasPending && !IsProcessing;
            return result;
        }
    }
    
    /// <summary>
    /// Gets whether configuration is valid.
    /// </summary>
    public bool IsConfigurationValid
    {
        get => _isConfigurationValid;
        private set
        {
            if (SetProperty(ref _isConfigurationValid, value))
            {
                OnPropertyChanged(nameof(CanStartProcessing));
                RaiseCanExecuteChanged();
            }
        }
    }
    
    /// <summary>
    /// Gets the configuration error message.
    /// </summary>
    public string ConfigurationError
    {
        get => _configurationError;
        private set => SetProperty(ref _configurationError, value);
    }

    #endregion

    #region Commands

    public ICommand AddFilesCommand { get; }
    public ICommand RemoveSelectedCommand { get; }
    public ICommand StartProcessingCommand { get; }
    public ICommand PauseProcessingCommand { get; }
    public ICommand StopProcessingCommand { get; }
    public ICommand ClearCompletedCommand { get; }
    public ICommand OpenOutputFolderCommand { get; }
    public ICommand OpenSettingsCommand { get; }
    public ICommand ViewJobDetailsCommand { get; }
    public ICommand ClearLogCommand { get; }
    public ICommand SaveLogCommand { get; }
    public ICommand ValidateConfigurationCommand { get; }

    #endregion

    #region Command Implementations

    private void AddFiles()
    {
        try
        {
            // TODO: Implement OpenFileDialog to select video files
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Video Files|*.mp4;*.avi;*.mkv;*.mov;*.wmv;*.flv;*.mpeg;*.mpg|All Files|*.*",
                Multiselect = true,
                Title = "Select Video Files"
            };

            if (dialog.ShowDialog() == true)
            {
                foreach (var fileName in dialog.FileNames)
                {
                    var job = new TranscriptionJob
                    {
                        InputFilePath = fileName,
                        OutputDirectory = System.IO.Path.GetDirectoryName(fileName) ?? string.Empty,
                        Status = JobStatus.Pending
                    };

                    Jobs.Add(new TranscriptionJobViewModel(job));
                    AddLog($"Added: {System.IO.Path.GetFileName(fileName)}");
                }

                UpdateStatistics();
                RaiseCanExecuteChanged();
            }
        }
        catch (Exception ex)
        {
            Utilities.WriteToLog(ex);
            _logService?.LogError($"Error adding files: {ex.Message}", ex);
            AddLog($"ERROR: Failed to add files - {ex.Message}");
        }
    }

    private void RemoveSelected()
    {
        try
        {
            var selectedJobs = Jobs.Where(j => j.IsSelected).ToList();
            foreach (var job in selectedJobs)
            {
                Jobs.Remove(job);
                AddLog($"Removed: {job.FileName}");
            }

            UpdateStatistics();
            RaiseCanExecuteChanged();
        }
        catch (Exception ex)
        {
            Utilities.WriteToLog(ex);
            _logService?.LogError($"Error removing selected jobs: {ex.Message}", ex);
            AddLog($"ERROR: Failed to remove jobs - {ex.Message}");
        }
    }

    private async System.Threading.Tasks.Task StartProcessingAsync()
    {
        AddLog("StartProcessingAsync called - DEBUG");
        
        // Check if services are available
        if (_jobOrchestrator == null || _jobQueueService == null)
        {
            AddLog($"Service check: orchestrator={_jobOrchestrator != null}, queue={_jobQueueService != null}");
            MessageBox.Show(
                "Job orchestrator not available. Please restart the application.", 
                "Service Error", 
                MessageBoxButton.OK, 
                MessageBoxImage.Error);
            return;
        }

        // Get file count for dialog
        var pendingJobs = Jobs.Where(j => j.Status == JobStatus.Pending).ToList();
        if (pendingJobs.Count == 0)
        {
            MessageBox.Show("No pending jobs to process.", "No Jobs", 
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        // Show processing mode dialog
        var dialog = new ProcessingModeDialog(pendingJobs.Count);
        
        // Get main window as owner
        var mainWindow = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
        if (mainWindow != null)
        {
            dialog.Owner = mainWindow;
        }
        
        if (dialog.ShowDialog() != true)
        {
            return; // User cancelled
        }

        try
        {
            // Enqueue pending jobs
            var jobsToProcess = pendingJobs.Select(vm => vm.Job).ToList();
            _jobQueueService.EnqueueJobs(jobsToProcess);
            
            var mode = dialog.IsSequentialMode ? ProcessingMode.Sequential : ProcessingMode.Parallel;
            AddLog($"Starting processing in {mode} mode with {pendingJobs.Count} job(s)...");
            
            // Start orchestrator
            await _jobOrchestrator.StartProcessingAsync(mode);
            
            AddLog("Processing completed.");
        }
        catch (Exception ex)
        {
            Utilities.WriteToLog(ex);
            AddLog($"Error: {ex.Message}");
            MessageBox.Show($"Processing error: {ex.Message}", "Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async System.Threading.Tasks.Task SimulateProcessingAsync()
    {
        AddLog("Simulating processing (demo mode)...");
        
        var pendingJobs = Jobs.Where(j => j.Status == JobStatus.Pending).ToList();
        
        foreach (var job in pendingJobs)
        {
            job.UpdateStatus(JobStatus.Converting, "Converting to WAV...");
            UpdateStatistics();
            await System.Threading.Tasks.Task.Delay(1000);
            
            job.SetProgress(33);
            job.UpdateStatus(JobStatus.Transcribing, "Transcribing audio...");
            UpdateStatistics();
            await System.Threading.Tasks.Task.Delay(2000);
            
            job.SetProgress(66);
            await System.Threading.Tasks.Task.Delay(1000);
            
            job.SetProgress(100);
            job.UpdateStatus(JobStatus.Completed, "Completed");
            UpdateStatistics();
            AddLog($"✓ Simulated completion: {job.FileName}");
        }
        
        MessageBox.Show("Simulation completed!", "Demo Mode", 
            MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void PauseProcessing()
    {
        try
        {
            if (_jobOrchestrator != null)
            {
                _ = _jobOrchestrator.PauseAsync();
                AddLog("Processing paused");
            }
        }
        catch (Exception ex)
        {
            Utilities.WriteToLog(ex);
            _logService?.LogError($"Error pausing processing: {ex.Message}", ex);
            AddLog($"ERROR: Failed to pause - {ex.Message}");
        }
    }

    private void StopProcessing()
    {
        try
        {
            if (_jobOrchestrator != null)
            {
                _ = _jobOrchestrator.CancelAsync();
                AddLog("Processing stopped - cancelling all jobs...");
            }
        }
        catch (Exception ex)
        {
            Utilities.WriteToLog(ex);
            _logService?.LogError($"Error stopping processing: {ex.Message}", ex);
            AddLog($"ERROR: Failed to stop - {ex.Message}");
        }
    }

    private void ClearCompleted()
    {
        try
        {
            var completedJobs = Jobs.Where(j => j.IsCompleted).ToList();
            foreach (var job in completedJobs)
            {
                Jobs.Remove(job);
            }

            AddLog($"Cleared {completedJobs.Count} completed job(s)");
            UpdateStatistics();
            RaiseCanExecuteChanged();
        }
        catch (Exception ex)
        {
            Utilities.WriteToLog(ex);
            _logService?.LogError($"Error clearing completed jobs: {ex.Message}", ex);
            AddLog($"ERROR: Failed to clear completed jobs - {ex.Message}");
        }
    }

    private void OpenOutputFolder()
    {
        try
        {
            // TODO: Open default output folder
            MessageBox.Show("Output folder will be opened", "Open Output Folder", 
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            Utilities.WriteToLog(ex);
            _logService?.LogError($"Error opening output folder: {ex.Message}", ex);
            AddLog($"ERROR: Failed to open output folder - {ex.Message}");
        }
    }

    public void OpenSettings()
    {
        try
        {
            if (_settingsService == null)
            {
                AddLog("⚠️ Settings service not available");
                return;
            }
            
            // Open SettingsWindow with injected service
            var settingsWindow = new SettingsWindow(_settingsService);
            
            // Get main window as owner
            var mainWindow = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
            if (mainWindow != null)
            {
                settingsWindow.Owner = mainWindow;
            }
            
            // Show dialog
            var result = settingsWindow.ShowDialog();
            
            // Re-validate configuration after settings changed
            if (result == true)
            {
                AddLog("Settings saved - Revalidating configuration...");
                ValidateConfiguration();
            }
        }
        catch (Exception ex)
        {
            Utilities.WriteToLog(ex);
            _logService?.LogError($"Error opening settings: {ex.Message}", ex);
            AddLog($"ERROR: Failed to open settings - {ex.Message}");
        }
    }

    private void ViewJobDetails(TranscriptionJobViewModel? job)
    {
        try
        {
            if (job != null)
            {
                // TODO: Open JobDetailsDialog
                AddLog($"Viewing details for: {job.FileName}");
            }
        }
        catch (Exception ex)
        {
            Utilities.WriteToLog(ex);
            _logService?.LogError($"Error viewing job details: {ex.Message}", ex);
        }
    }

    private void ClearLog()
    {
        try
        {
            LogEntries.Clear();
            LogText = string.Empty;
        }
        catch (Exception ex)
        {
            Utilities.WriteToLog(ex);
            _logService?.LogError($"Error clearing log: {ex.Message}", ex);
        }
    }

    private void SaveLog()
    {
        try
        {
            // TODO: Implement save log to file
            MessageBox.Show("Log will be saved to file", "Save Log", 
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            Utilities.WriteToLog(ex);
            _logService?.LogError($"Error saving log: {ex.Message}", ex);
            AddLog($"ERROR: Failed to save log - {ex.Message}");
        }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Adds a log entry with timestamp.
    /// </summary>
    public void AddLog(string message)
    {
        try
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            var logEntry = $"[{timestamp}] {message}";
            LogEntries.Add(logEntry);
            LogText += logEntry + Environment.NewLine;

            // Keep only last 1000 entries
            while (LogEntries.Count > 1000)
            {
                LogEntries.RemoveAt(0);
            }
            
            // Log to service if available
            _logService?.LogInfo(message);
        }
        catch (Exception ex)
        {
            Utilities.WriteToLog(ex);
            // Can't log to AddLog since we're in AddLog - just write to service directly
            _logService?.LogError($"Error in AddLog: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Updates job statistics.
    /// </summary>
    private void UpdateStatistics()
    {
        try
        {
            ProcessingCount = Jobs.Count(j => j.Status == JobStatus.Converting || j.Status == JobStatus.Transcribing);
            CompletedCount = Jobs.Count(j => j.IsCompleted);
            FailedCount = Jobs.Count(j => j.IsFailed);
            
            OnPropertyChanged(nameof(TotalJobs));
            OnPropertyChanged(nameof(OverallProgress));
            OnPropertyChanged(nameof(HasCompletedJobs));
            OnPropertyChanged(nameof(CanStartProcessing));
        }
        catch (Exception ex)
        {
            Utilities.WriteToLog(ex);
            _logService?.LogError($"Error updating statistics: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Raises CanExecuteChanged for all commands.
    /// </summary>
    private void RaiseCanExecuteChanged()
    {
        try
        {
            (StartProcessingCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
            (RemoveSelectedCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (PauseProcessingCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (StopProcessingCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (ClearCompletedCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }
        catch (Exception ex)
        {
            Utilities.WriteToLog(ex);
            _logService?.LogError($"Error raising CanExecuteChanged: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Adds test data for development/testing.
    /// </summary>
    private void AddTestData()
    {
        // DISABLED: Use real files with "Add Files" button
        // Add some sample jobs for testing UI
        /*
        var testJobs = new[]
        {
            new TranscriptionJob { InputFilePath = @"C:\Videos\Video1.mp4", Status = JobStatus.Pending, Progress = 0 },
            new TranscriptionJob { InputFilePath = @"C:\Videos\Video2.mpeg", Status = JobStatus.Converting, Progress = 45, StartTime = DateTime.Now.AddMinutes(-5) },
            new TranscriptionJob { InputFilePath = @"C:\Videos\Video3.avi", Status = JobStatus.Transcribing, Progress = 75, StartTime = DateTime.Now.AddMinutes(-10) },
            new TranscriptionJob { InputFilePath = @"C:\Videos\Video4.mkv", Status = JobStatus.Completed, Progress = 100, StartTime = DateTime.Now.AddMinutes(-20), EndTime = DateTime.Now.AddMinutes(-2) },
        };

        foreach (var job in testJobs)
        {
            Jobs.Add(new TranscriptionJobViewModel(job));
        }

        UpdateStatistics();
        AddLog($"Loaded {Jobs.Count} test job(s)");
        */
        
        AddLog("Application started - Click 'Add Files' to begin");
    }

    #endregion

    #region Event Handlers

    /// <summary>
    /// Handles job progress updates from orchestrator
    /// </summary>
    private void OnJobProgressChanged(object? sender, JobProgressEventArgs e)
    {
        try
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                // Find matching job VM
                var jobVM = Jobs.FirstOrDefault(j => j.Job.Id == e.Job.Id);
                if (jobVM != null)
                {
                    // Update progress
                    jobVM.SetProgress(e.Progress.Percent, e.Progress.Message);
                    jobVM.CurrentPhase = e.Progress.Phase;
                    
                    // Update status based on phase
                    if (e.Progress.Phase == ProcessingPhase.Converting)
                    {
                        jobVM.UpdateStatus(JobStatus.Converting, e.Progress.Message);
                    }
                    else if (e.Progress.Phase == ProcessingPhase.Transcribing)
                    {
                        jobVM.UpdateStatus(JobStatus.Transcribing, e.Progress.Message);
                    }
                    
                    UpdateStatistics();
                }
            });
        }
        catch (Exception ex)
        {
            Utilities.WriteToLog(ex);
            _logService?.LogError($"Error handling job progress: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Handles job completion from orchestrator
    /// </summary>
    private void OnJobCompleted(object? sender, JobCompletedEventArgs e)
    {
        try
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var jobVM = Jobs.FirstOrDefault(j => j.Job.Id == e.Job.Id);
                if (jobVM != null)
                {
                    // Update from job model
                    jobVM.UpdateStatus(e.Job.Status, e.Job.ErrorMessage ?? "");
                    jobVM.SetProgress(e.Job.Progress);
                
                    if (e.IsSuccess)
                    {
                        AddLog($"✓ Completed: {jobVM.FileName}");
                    }
                    else
                    {
                        AddLog($"✗ Failed: {jobVM.FileName} - {e.Job.ErrorMessage}");
                    }
                    
                    UpdateStatistics();
                    RaiseCanExecuteChanged();
                }
            });
        }
        catch (Exception ex)
        {
            Utilities.WriteToLog(ex);
            _logService?.LogError($"Error handling job completion: {ex.Message}", ex);
        }
    }
    
    /// <summary>
    /// Initialize configuration on app startup - ALWAYS ensure settings.json exists:
    /// 1. Load settings (auto-detect if file doesn't exist)
    /// 2. Validate paths
    /// 3. If invalid, force auto-detect again
    /// 4. ALWAYS save settings.json
    /// </summary>
    private void InitializeConfiguration()
    {
        try
        {
            AddLog("Application started - Click 'Add Files' to begin");
            
            // Check if first run
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var settingsPath = Path.Combine(appDataPath, "VideoSubtitleGenerator", "settings.json");
            bool isFirstRun = !System.IO.File.Exists(settingsPath);
            
            if (isFirstRun)
            {
                AddLog("🎉 Welcome to Video Subtitle Generator!");
                AddLog("⚙️ First run detected - Auto-configuring...");
            }
            
            // Step 1: Load settings (triggers auto-detect if needed)
            var settings = _settingsService.LoadSettings();
            
            // Step 2: Validate paths
            bool pythonOk = !string.IsNullOrWhiteSpace(settings.Python.PythonExePath) && 
                           System.IO.File.Exists(settings.Python.PythonExePath);
            
            var scriptPath = settings.Python.ScriptPath;
            if (!System.IO.Path.IsPathRooted(scriptPath))
            {
                scriptPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, scriptPath);
            }
            bool scriptOk = !string.IsNullOrWhiteSpace(settings.Python.ScriptPath) && 
                           System.IO.File.Exists(scriptPath);
            
            // Step 3: If invalid, force re-detection
            if (!pythonOk || !scriptOk)
            {
                AddLog("⚠️ Invalid configuration - Attempting auto-detection...");
                settings = _settingsService.GetDefaultSettings();
                
                // Re-validate
                pythonOk = !string.IsNullOrWhiteSpace(settings.Python.PythonExePath) && 
                          System.IO.File.Exists(settings.Python.PythonExePath);
                
                scriptPath = settings.Python.ScriptPath;
                if (!System.IO.Path.IsPathRooted(scriptPath))
                {
                    scriptPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, scriptPath);
                }
                scriptOk = !string.IsNullOrWhiteSpace(settings.Python.ScriptPath) && 
                          System.IO.File.Exists(scriptPath);
            }
            
            // Step 4: Show results
            if (pythonOk && scriptOk)
            {
                AddLog($"✓ Python: {settings.Python.PythonExePath}");
                AddLog($"✓ Script: {settings.Python.ScriptPath}");
                
                if (settings.FFmpeg.UseBundled)
                {
                    AddLog("✓ FFmpeg: Using bundled version");
                }
                else
                {
                    AddLog($"✓ FFmpeg: {settings.FFmpeg.ExecutablePath}");
                }
            }
            else
            {
                AddLog("⚠️ Auto-detection incomplete:");
                if (!pythonOk) AddLog("  ✗ Python not found");
                if (!scriptOk) AddLog("  ✗ Script not found");
                AddLog("💡 Please open Settings to configure manually");
            }
            
            // Step 5: ALWAYS save settings.json (ensure file always exists)
            _settingsService.SaveSettingsAsync(settings).GetAwaiter().GetResult();
            AddLog("✓ Configuration saved");
            
            // Check output directory
            if (string.IsNullOrWhiteSpace(settings.Processing.OutputDirectory))
            {
                AddLog("💡 Output directory not set - subtitles will be saved with video files");
            }
            
            // Final validation for UI
            ValidateConfiguration();
        }
        catch (Exception ex)
        {
            Utilities.WriteToLog(ex);
            AddLog($"❌ Initialization error: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Checks if this is first run and shows auto-detection results
    /// DEPRECATED: Use InitializeConfiguration() instead
    /// </summary>
    private void CheckFirstRun()
    {
        try
        {
            // Check if settings file exists
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var settingsPath = Path.Combine(appDataPath, "VideoSubtitleGenerator", "settings.json");
            
            if (!System.IO.File.Exists(settingsPath))
            {
                AddLog("🎉 Welcome to Video Subtitle Generator!");
                AddLog("⚙️ First run detected - Auto-configuring...");
                
                // Load settings (will trigger auto-detection)
                var settings = _settingsService?.LoadSettings();
                
                if (settings != null)
                {
                    // Show what was detected
                    AddLog($"🔍 Auto-detected Python: {settings.Python.PythonExePath}");
                    
                    if (settings.FFmpeg.UseBundled)
                    {
                        AddLog("🔍 FFmpeg: Using bundled version");
                    }
                    else
                    {
                        AddLog($"🔍 Auto-detected FFmpeg: {settings.FFmpeg.ExecutablePath}");
                    }
                    
                    AddLog($"🔍 Auto-detected Script: {settings.Python.ScriptPath}");
                    AddLog("");
                    AddLog("💡 Tip: If configuration is invalid, click Settings to adjust paths manually.");
                    
                    // Save the auto-detected settings
                    try
                    {
                        _settingsService?.SaveSettingsAsync(settings).GetAwaiter().GetResult();
                        AddLog("✓ Auto-configuration saved");
                    }
                    catch (Exception saveEx)
                    {
                        Utilities.WriteToLog(saveEx);
                        AddLog($"⚠️ Failed to save auto-configuration: {saveEx.Message}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Utilities.WriteToLog(ex);
            AddLog($"⚠️ Auto-configuration warning: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Manually trigger configuration validation and show result dialog
    /// </summary>
    private void ValidateConfigurationManual()
    {
        try
        {
            // Run validation
            ValidateConfiguration();
            
            // Show result dialog
            var settings = _settingsService?.LoadSettings();
            if (settings == null)
            {
                MessageBox.Show(
                    "Unable to load settings configuration.",
                    "Configuration Check",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }
            
            var messages = new List<string>();
            
            // Check Python
            if (!string.IsNullOrWhiteSpace(settings.Python.PythonExePath))
            {
                if (System.IO.File.Exists(settings.Python.PythonExePath))
                {
                    messages.Add($"✓ Python: {settings.Python.PythonExePath}");
                }
                else
                {
                    messages.Add($"✗ Python: Not found at {settings.Python.PythonExePath}");
                }
            }
            else
            {
                messages.Add("✗ Python: Not configured");
            }
            
            // Check FFmpeg
            if (settings.FFmpeg.UseBundled)
            {
                messages.Add("✓ FFmpeg: Using bundled version");
            }
            else if (!string.IsNullOrWhiteSpace(settings.FFmpeg.ExecutablePath))
            {
                if (System.IO.File.Exists(settings.FFmpeg.ExecutablePath))
                {
                    messages.Add($"✓ FFmpeg: {settings.FFmpeg.ExecutablePath}");
                }
                else
                {
                    messages.Add($"✗ FFmpeg: Not found at {settings.FFmpeg.ExecutablePath}");
                }
            }
            else
            {
                messages.Add("✗ FFmpeg: Not configured");
            }
            
            // Check Script
            if (!string.IsNullOrWhiteSpace(settings.Python.ScriptPath))
            {
                var scriptPath = settings.Python.ScriptPath;
                if (!System.IO.Path.IsPathRooted(scriptPath))
                {
                    scriptPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, scriptPath);
                }
                
                if (System.IO.File.Exists(scriptPath))
                {
                    messages.Add($"✓ Script: {settings.Python.ScriptPath}");
                }
                else
                {
                    messages.Add($"✗ Script: Not found at {settings.Python.ScriptPath}");
                }
            }
            else
            {
                messages.Add("✗ Script: Not configured");
            }
            
            // Check Output Directory (optional)
            if (string.IsNullOrWhiteSpace(settings.Processing.OutputDirectory))
            {
                messages.Add("✓ Output Directory: Will save with video files");
            }
            else if (System.IO.Directory.Exists(settings.Processing.OutputDirectory))
            {
                messages.Add($"✓ Output Directory: {settings.Processing.OutputDirectory}");
            }
            else
            {
                messages.Add($"⚠ Output Directory: Will be created - {settings.Processing.OutputDirectory}");
            }
            
            var message = "Configuration verification:\n\n" + string.Join("\n", messages);
            
            MessageBox.Show(
                message,
                "Configuration Check",
                MessageBoxButton.OK,
                IsConfigurationValid ? MessageBoxImage.Information : MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            Utilities.WriteToLog(ex);
            _logService?.LogError($"Error in ValidateConfigurationManual: {ex.Message}", ex);
            MessageBox.Show(
                $"Error validating configuration:\n{ex.Message}",
                "Configuration Check Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
    
    /// <summary>
    /// Validates system configuration (Python, FFmpeg, Output Directory)
    /// </summary>
    private void ValidateConfiguration()
    {
        var errors = new List<string>();
        
        try
        {
            // Load settings
            var settings = _settingsService?.LoadSettings();
            
            if (settings == null)
            {
                errors.Add("Unable to load settings");
                IsConfigurationValid = false;
                ConfigurationError = string.Join("; ", errors);
                AddLog("⚠️ Configuration validation failed: Unable to load settings");
                AddLog("Please open Settings and configure Python/FFmpeg paths");
                return;
            }
            
            // Check Python
            if (string.IsNullOrWhiteSpace(settings.Python.PythonExePath))
            {
                errors.Add("Python path not configured");
            }
            else if (!System.IO.File.Exists(settings.Python.PythonExePath))
            {
                errors.Add($"Python not found: {settings.Python.PythonExePath}");
            }
            
            // Check FFmpeg
            if (string.IsNullOrWhiteSpace(settings.FFmpeg.ExecutablePath))
            {
                errors.Add("FFmpeg path not configured");
            }
            else if (!System.IO.File.Exists(settings.FFmpeg.ExecutablePath))
            {
                errors.Add($"FFmpeg not found: {settings.FFmpeg.ExecutablePath}");
            }
            
            // Check Python Script
            if (string.IsNullOrWhiteSpace(settings.Python.ScriptPath))
            {
                errors.Add("Python script path not configured");
            }
            else
            {
                // Resolve relative path if needed
                var scriptPath = settings.Python.ScriptPath;
                if (!System.IO.Path.IsPathRooted(scriptPath))
                {
                    scriptPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, scriptPath);
                }
                
                if (!System.IO.File.Exists(scriptPath))
                {
                    errors.Add($"Python script not found: {settings.Python.ScriptPath}");
                }
            }
            
            // Check Output Directory (optional - can be empty to use video directory)
            if (!string.IsNullOrWhiteSpace(settings.Processing.OutputDirectory))
            {
                // Only validate if a directory is specified
                try
                {
                    // Try to create directory if it doesn't exist
                    if (!System.IO.Directory.Exists(settings.Processing.OutputDirectory))
                    {
                        System.IO.Directory.CreateDirectory(settings.Processing.OutputDirectory);
                        AddLog($"Created output directory: {settings.Processing.OutputDirectory}");
                    }
                }
                catch (Exception ex)
                {
                    Utilities.WriteToLog(ex);
                    // Only warn, don't block - can still use video directory
                    AddLog($"⚠️ Warning: Cannot create output directory '{settings.Processing.OutputDirectory}': {ex.Message}");
                    AddLog("Subtitles will be saved in the same folder as video files");
                }
            }
            else
            {
                AddLog("Output directory not specified - subtitles will be saved with video files");
            }
            
            // Update validation state
            IsConfigurationValid = errors.Count == 0;
            ConfigurationError = string.Join("; ", errors);
            
            if (IsConfigurationValid)
            {
                AddLog("✓ Configuration validated successfully");
            }
            else
            {
                AddLog("⚠️ Configuration validation failed:");
                foreach (var error in errors)
                {
                    AddLog($"  - {error}");
                }
                AddLog("Please open Settings to fix configuration issues");
                
                // Show warning dialog
                MessageBox.Show(
                    $"Configuration errors detected:\n\n{string.Join("\n", errors)}\n\nPlease configure settings before starting.",
                    "Configuration Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }
        catch (Exception ex)
        {
            Utilities.WriteToLog(ex);
            IsConfigurationValid = false;
            ConfigurationError = $"Validation error: {ex.Message}";
            AddLog($"⚠️ Configuration validation error: {ex.Message}");
        }
    }

    #endregion
}

