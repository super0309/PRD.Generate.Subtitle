# Architecture Overview - Video Subtitle Generator

## 🏛️ Kiến trúc tổng thể

### Clean Architecture Layers

```
┌─────────────────────────────────────────────────────────┐
│                      Presentation Layer                  │
│              (VideoSubtitleGenerator.UI.Wpf)            │
│                    WPF + MVVM Pattern                    │
└───────────────────┬─────────────────────────────────────┘
                    │ References
                    ↓
┌─────────────────────────────────────────────────────────┐
│                       Core Layer                         │
│             (VideoSubtitleGenerator.Core)               │
│        Models, Interfaces, Business Logic               │
└───────────────────┬─────────────────────────────────────┘
                    ↑ Implements Interfaces
                    │
┌─────────────────────────────────────────────────────────┐
│                  Infrastructure Layer                    │
│          (VideoSubtitleGenerator.Infrastructure)        │
│     External Services, File I/O, Process Execution      │
└───────────────────┬─────────────────────────────────────┘
                    │ Calls
                    ↓
┌─────────────────────────────────────────────────────────┐
│                     Python Worker                        │
│              (process_media.py + FFmpeg)                │
│          Audio Extraction + AI Transcription            │
└─────────────────────────────────────────────────────────┘
```

## 📦 Project Dependencies

```
UI.Wpf
  ↓ (references)
Core ← Infrastructure
  ↓ (implements)
Interfaces
```

### Dependency Rules:
- ✅ UI → Core
- ✅ UI → Infrastructure (for DI registration)
- ✅ Infrastructure → Core
- ❌ Core → Infrastructure (NEVER)
- ❌ Core → UI (NEVER)

## 🎯 Core Layer - Domain Models

### TranscriptionJob
```csharp
public class TranscriptionJob
{
    public Guid Id { get; set; }
    public string InputFilePath { get; set; }
    public string OutputDirectory { get; set; }
    public JobStatus Status { get; set; }
    public int Progress { get; set; }  // 0-100
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string ErrorMessage { get; set; }
    public TranscriptionResult Result { get; set; }
    public WhisperSettings Settings { get; set; }
}
```

### JobStatus (Enum)
```csharp
public enum JobStatus
{
    Pending,        // Chờ xử lý
    Converting,     // Đang convert sang WAV
    Transcribing,   // Đang transcribe với Whisper
    Completed,      // Hoàn thành
    Failed,         // Lỗi
    Canceled        // User hủy
}
```

### TranscriptionResult
```csharp
public class TranscriptionResult
{
    public bool IsSuccess { get; set; }
    public string WavFilePath { get; set; }
    public string SubtitleFilePath { get; set; }
    public TimeSpan Duration { get; set; }
    public string ErrorMessage { get; set; }
    public Dictionary<string, string> Metadata { get; set; }
}
```

### WhisperSettings
```csharp
public class WhisperSettings
{
    public string Model { get; set; } = "small";
    public string Language { get; set; } = "English";
    public string Device { get; set; } = "cpu";
    public bool Fp16 { get; set; } = false;
    public string Task { get; set; } = "transcribe";
    public string OutputFormat { get; set; } = "srt";
}
```

## 🔌 Core Interfaces

### IPythonWorkerService
```csharp
public interface IPythonWorkerService
{
    Task<TranscriptionResult> ProcessAsync(
        TranscriptionJob job, 
        IProgress<int> progress = null,
        CancellationToken cancellationToken = default
    );
    
    Task<bool> ValidateEnvironmentAsync();
    string GetPythonVersion();
}
```

### IJobQueueService
```csharp
public interface IJobQueueService
{
    void EnqueueJobs(IEnumerable<TranscriptionJob> jobs);
    TranscriptionJob DequeueJob();
    IReadOnlyList<TranscriptionJob> GetAllJobs();
    TranscriptionJob GetJobById(Guid id);
    void UpdateJob(TranscriptionJob job);
    void ClearCompleted();
    void CancelAll();
    
    event EventHandler<JobEventArgs> JobStatusChanged;
}
```

### IJobOrchestrator
```csharp
public interface IJobOrchestrator
{
    Task StartProcessingAsync(CancellationToken cancellationToken = default);
    Task PauseAsync();
    Task ResumeAsync();
    Task CancelAsync();
    
    bool IsRunning { get; }
    int ActiveWorkers { get; }
    
    event EventHandler<JobProgressEventArgs> ProgressChanged;
    event EventHandler<JobCompletedEventArgs> JobCompleted;
}
```

### ISettingsService
```csharp
public interface ISettingsService
{
    AppSettings LoadSettings();
    Task SaveSettingsAsync(AppSettings settings);
    AppSettings GetDefaultSettings();
}
```

### ILogService
```csharp
public interface ILogService
{
    void LogInfo(string message);
    void LogWarning(string message);
    void LogError(string message, Exception exception = null);
    void LogDebug(string message);
    
    IEnumerable<LogEntry> GetRecentLogs(int count = 100);
    Task ClearLogsAsync();
}
```

## 🏗️ Infrastructure Implementation

### PythonWorkerService Implementation Strategy

```csharp
public class PythonWorkerService : IPythonWorkerService
{
    private readonly AppSettings _settings;
    private readonly ILogService _logger;
    
    public async Task<TranscriptionResult> ProcessAsync(
        TranscriptionJob job,
        IProgress<int> progress,
        CancellationToken cancellationToken)
    {
        // 1. Build command
        var command = BuildPythonCommand(job);
        
        // 2. Start process
        using var process = new Process();
        // ... configure ProcessStartInfo
        
        // 3. Capture output
        var outputBuilder = new StringBuilder();
        process.OutputDataReceived += (s, e) => {
            if (e.Data != null) {
                outputBuilder.AppendLine(e.Data);
                // Parse progress if possible
                TryParseProgress(e.Data, progress);
            }
        };
        
        // 4. Wait for completion
        await process.WaitForExitAsync(cancellationToken);
        
        // 5. Parse result JSON from stdout
        return ParseResult(outputBuilder.ToString());
    }
    
    private string BuildPythonCommand(TranscriptionJob job)
    {
        return $@"
            {_settings.Python.PythonExePath} 
            {_settings.Python.ScriptPath} 
            --input ""{job.InputFilePath}"" 
            --output-dir ""{job.OutputDirectory}""
            --model {job.Settings.Model}
            --language {job.Settings.Language}
            --device {job.Settings.Device}
        ".Trim();
    }
}
```

## 🎨 MVVM Pattern - UI Layer

### MainViewModel
```csharp
public class MainViewModel : ViewModelBase
{
    private readonly IJobQueueService _jobQueue;
    private readonly IJobOrchestrator _orchestrator;
    private readonly ISettingsService _settingsService;
    
    // Observable Collections
    public ObservableCollection<TranscriptionJobViewModel> Jobs { get; }
    public ObservableCollection<string> LogMessages { get; }
    
    // Properties
    public WhisperSettings CurrentSettings { get; set; }
    public bool IsProcessing { get; set; }
    public int OverallProgress { get; set; }
    
    // Commands
    public ICommand AddFilesCommand { get; }
    public ICommand RemoveSelectedCommand { get; }
    public ICommand ClearAllCommand { get; }
    public ICommand StartProcessingCommand { get; }
    public ICommand CancelProcessingCommand { get; }
    public ICommand OpenSettingsCommand { get; }
    public ICommand BrowseOutputDirectoryCommand { get; }
}
```

### TranscriptionJobViewModel
```csharp
public class TranscriptionJobViewModel : ViewModelBase
{
    private readonly TranscriptionJob _model;
    
    public string FileName => Path.GetFileName(_model.InputFilePath);
    public string Status => _model.Status.ToString();
    public int Progress => _model.Progress;
    public string StatusColor => GetStatusColor(_model.Status);
    
    // Commands for individual job
    public ICommand OpenOutputFolderCommand { get; }
    public ICommand RetryCommand { get; }
    public ICommand CancelCommand { get; }
}
```

## 🔄 Data Flow Sequence

### Scenario: User starts processing 3 video files

```
[User Action]
    ↓
[MainViewModel.StartProcessingCommand]
    ↓
[JobQueueService.EnqueueJobs(jobs)]
    ↓
[JobOrchestrator.StartProcessingAsync()]
    ↓ (creates worker tasks based on MaxParallelJobs)
    ├─→ [Worker 1] → Job A
    │       ↓
    │   [PythonWorkerService.ProcessAsync(jobA)]
    │       ↓
    │   [Python: process_media.py]
    │       ├─→ FFmpeg convert
    │       └─→ Whisper transcribe
    │       ↓
    │   [Returns TranscriptionResult]
    │       ↓
    │   [JobOrchestrator updates Job A status]
    │       ↓
    │   [Event: JobCompleted fired]
    │       ↓
    │   [MainViewModel updates UI]
    │
    ├─→ [Worker 2] → Job B (parallel)
    │
    └─→ [Worker queue] → Job C (waits for worker)
```

## 🐍 Python Worker Interface

### Input (Command Line Args)
```bash
python process_media.py \
  --input "C:\path\to\video.mp4" \
  --output-dir "C:\path\to\output" \
  --model small \
  --language English \
  --device cpu \
  --fp16 False
```

### Output (JSON to stdout)
```json
{
  "status": "success",
  "wav_file": "C:\\path\\to\\output\\video.wav",
  "subtitle_file": "C:\\path\\to\\output\\video.srt",
  "duration_seconds": 125.5,
  "error": null,
  "metadata": {
    "model": "small",
    "language": "en",
    "processing_time": 45.2
  }
}
```

### Error Output
```json
{
  "status": "error",
  "error": "FFmpeg conversion failed: File not found",
  "wav_file": null,
  "subtitle_file": null
}
```

## 🔐 Error Handling Strategy

### Tầng UI:
- Display user-friendly messages
- Show retry options
- Log to UI console

### Tầng Core:
- Validate business rules
- Throw custom exceptions

### Tầng Infrastructure:
- Catch external process errors
- Retry logic cho transient failures
- Wrap exceptions với context

### Tầng Python:
- Try-catch all operations
- Always output JSON (success or error)
- Exit codes: 0 = success, 1 = error

## 📊 Progress Reporting Strategy

### Level 1: Job-level progress
- Pending → Converting → Transcribing → Completed

### Level 2: Detailed progress (optional)
- FFmpeg: Parse progress từ stderr
- Whisper: Parse log output nếu có

### Level 3: Overall progress
```csharp
OverallProgress = (CompletedJobs / TotalJobs) * 100
```

## 🧪 Testing Strategy

### Unit Tests (Core):
- JobQueueService logic
- Job state transitions
- Settings validation

### Integration Tests (Infrastructure):
- PythonWorkerService với mock Python script
- Process execution và output parsing

### UI Tests:
- ViewModel logic
- Command execution
- Property change notifications

### End-to-End Tests:
- Thực tế chạy với video mẫu nhỏ

## 🚀 Performance Considerations

- **Parallel Processing**: MaxParallelJobs = CPU cores - 1
- **Memory**: Limit số job trong queue
- **Async/Await**: Không block UI thread
- **IProgress<T>**: Update UI từ background threads safely
- **Cancellation**: Support CancellationToken everywhere
- **Resource Cleanup**: Dispose Process objects properly
