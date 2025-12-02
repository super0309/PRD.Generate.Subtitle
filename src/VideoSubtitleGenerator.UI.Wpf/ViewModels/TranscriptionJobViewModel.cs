using System;
using System.IO;
using VideoSubtitleGenerator.Core.Enums;
using VideoSubtitleGenerator.Core.Models;

namespace VideoSubtitleGenerator.UI.Wpf.ViewModels;

/// <summary>
/// ViewModel wrapper for TranscriptionJob with UI-specific properties and notifications.
/// </summary>
public class TranscriptionJobViewModel : ViewModelBase
{
    private readonly TranscriptionJob _job;
    private bool _isSelected;
    private string _progressMessage = string.Empty;

    public TranscriptionJobViewModel(TranscriptionJob job)
    {
        _job = job ?? throw new ArgumentNullException(nameof(job));
        UpdateProgressMessage();
    }

    /// <summary>
    /// Gets the underlying TranscriptionJob model.
    /// </summary>
    public TranscriptionJob Job => _job;

    /// <summary>
    /// Gets the unique job ID.
    /// </summary>
    public Guid Id => _job.Id;

    /// <summary>
    /// Gets or sets the input file path.
    /// </summary>
    public string InputFilePath
    {
        get => _job.InputFilePath;
        set
        {
            if (_job.InputFilePath != value)
            {
                _job.InputFilePath = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(FileName));
            }
        }
    }

    /// <summary>
    /// Gets the file name without path.
    /// </summary>
    public string FileName => Path.GetFileName(InputFilePath);

    /// <summary>
    /// Gets or sets the output directory.
    /// </summary>
    public string OutputDirectory
    {
        get => _job.OutputDirectory;
        set
        {
            if (_job.OutputDirectory != value)
            {
                _job.OutputDirectory = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Gets or sets the job status.
    /// </summary>
    public JobStatus Status
    {
        get => _job.Status;
        set
        {
            if (_job.Status != value)
            {
                _job.Status = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanRetry));
                OnPropertyChanged(nameof(IsCompleted));
                OnPropertyChanged(nameof(IsFailed));
                UpdateProgressMessage();
            }
        }
    }

    /// <summary>
    /// Gets or sets the current processing phase.
    /// </summary>
    public ProcessingPhase CurrentPhase
    {
        get => _job.CurrentPhase;
        set
        {
            if (_job.CurrentPhase != value)
            {
                _job.CurrentPhase = value;
                OnPropertyChanged();
                UpdateProgressMessage();
            }
        }
    }

    /// <summary>
    /// Gets or sets the progress percentage (0-100).
    /// </summary>
    public int Progress
    {
        get => _job.Progress;
        set
        {
            if (_job.Progress != value)
            {
                _job.Progress = Math.Max(0, Math.Min(100, value));
                OnPropertyChanged();
                OnPropertyChanged(nameof(ProgressText));
                UpdateProgressMessage();
            }
        }
    }

    /// <summary>
    /// Gets formatted progress text.
    /// </summary>
    public string ProgressText => $"{Progress}%";

    /// <summary>
    /// Gets or sets the progress message (e.g., "Processing segment 3/10...").
    /// </summary>
    public string ProgressMessage
    {
        get => _progressMessage;
        set => SetProperty(ref _progressMessage, value);
    }

    /// <summary>
    /// Gets or sets the start time.
    /// </summary>
    public DateTime? StartTime
    {
        get => _job.StartTime;
        set
        {
            if (_job.StartTime != value)
            {
                _job.StartTime = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ElapsedTime));
                OnPropertyChanged(nameof(ElapsedTimeText));
            }
        }
    }

    /// <summary>
    /// Gets or sets the end time.
    /// </summary>
    public DateTime? EndTime
    {
        get => _job.EndTime;
        set
        {
            if (_job.EndTime != value)
            {
                _job.EndTime = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ElapsedTime));
                OnPropertyChanged(nameof(ElapsedTimeText));
            }
        }
    }

    /// <summary>
    /// Gets the elapsed time.
    /// </summary>
    public TimeSpan? ElapsedTime => _job.ElapsedTime;

    /// <summary>
    /// Gets formatted elapsed time text.
    /// </summary>
    public string ElapsedTimeText
    {
        get
        {
            if (ElapsedTime.HasValue)
            {
                var ts = ElapsedTime.Value;
                return ts.TotalHours >= 1 
                    ? ts.ToString(@"hh\:mm\:ss") 
                    : ts.ToString(@"mm\:ss");
            }
            return "--:--";
        }
    }

    /// <summary>
    /// Gets or sets the estimated time remaining.
    /// </summary>
    public TimeSpan? EstimatedTimeRemaining
    {
        get => _job.EstimatedTimeRemaining;
        set
        {
            if (_job.EstimatedTimeRemaining != value)
            {
                _job.EstimatedTimeRemaining = value;
                OnPropertyChanged();
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
            if (EstimatedTimeRemaining.HasValue)
            {
                var ts = EstimatedTimeRemaining.Value;
                return ts.TotalHours >= 1 
                    ? ts.ToString(@"hh\:mm\:ss") 
                    : ts.ToString(@"mm\:ss");
            }
            return "--:--";
        }
    }

    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    public string? ErrorMessage
    {
        get => _job.ErrorMessage;
        set
        {
            if (_job.ErrorMessage != value)
            {
                _job.ErrorMessage = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasError));
            }
        }
    }

    /// <summary>
    /// Gets whether the job has an error.
    /// </summary>
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    /// <summary>
    /// Gets whether the job can be retried (failed status).
    /// </summary>
    public bool CanRetry => Status == JobStatus.Failed;

    /// <summary>
    /// Gets whether the job is completed.
    /// </summary>
    public bool IsCompleted => Status == JobStatus.Completed;

    /// <summary>
    /// Gets whether the job has failed.
    /// </summary>
    public bool IsFailed => Status == JobStatus.Failed;

    /// <summary>
    /// Gets or sets whether this job is selected in the UI.
    /// </summary>
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    /// <summary>
    /// Gets the transcription result.
    /// </summary>
    public TranscriptionResult? Result => _job.Result;

    /// <summary>
    /// Gets the Whisper settings for this job.
    /// </summary>
    public WhisperSettings Settings => _job.Settings;

    /// <summary>
    /// Updates the progress message based on current status and phase.
    /// </summary>
    private void UpdateProgressMessage()
    {
        ProgressMessage = Status switch
        {
            JobStatus.Pending => "Waiting to start...",
            JobStatus.Converting => $"Converting to WAV... {Progress}%",
            JobStatus.Transcribing => $"Transcribing audio... {Progress}%",
            JobStatus.Completed => "Completed successfully",
            JobStatus.Failed => ErrorMessage ?? "Failed",
            JobStatus.Canceled => "Canceled by user",
            _ => string.Empty
        };
    }

    /// <summary>
    /// Updates all time-related properties (call periodically during processing).
    /// </summary>
    public void UpdateTimeProperties()
    {
        OnPropertyChanged(nameof(ElapsedTime));
        OnPropertyChanged(nameof(ElapsedTimeText));
    }

    /// <summary>
    /// Updates the job status and message.
    /// </summary>
    public void UpdateStatus(JobStatus newStatus, string? message = null)
    {
        Status = newStatus;
        if (message != null)
        {
            ProgressMessage = message;
        }
        
        if (newStatus == JobStatus.Converting || newStatus == JobStatus.Transcribing)
        {
            if (StartTime == null)
            {
                StartTime = DateTime.Now;
            }
        }
        else if (newStatus == JobStatus.Completed || newStatus == JobStatus.Failed || newStatus == JobStatus.Canceled)
        {
            EndTime = DateTime.Now;
        }
    }

    /// <summary>
    /// Sets the progress percentage and updates message.
    /// </summary>
    public void SetProgress(int progressPercent, string? message = null)
    {
        Progress = progressPercent;
        if (message != null)
        {
            ProgressMessage = message;
        }
    }

    /// <summary>
    /// Gets the status message for display.
    /// </summary>
    public string StatusMessage => ProgressMessage;
}
