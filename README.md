# Video Subtitle Generator

## 📖 Tổng quan dự án
Ứng dụng Windows Desktop (WPF) cho phép chọn hàng loạt file video, tự động chuyển đổi sang WAV và sử dụng Whisper AI để sinh phụ đề tự động.

## 🏗️ Kiến trúc 3 lớp

### 1. **UI Layer (WPF + MVVM)**
- Giao diện người dùng với WPF
- Pattern MVVM để tách biệt logic và UI
- Binding, Command, Converter

### 2. **Core/Domain Layer (.NET Class Library)**
- Business logic
- Models, Interfaces, Services
- Không phụ thuộc vào UI hay Infrastructure

### 3. **Infrastructure Layer (.NET Class Library)**
- Implement các interface từ Core
- Gọi Python worker, FFmpeg
- File operations, logging, configuration

### 4. **Python Worker (Separate)**
- Script Python xử lý FFmpeg + Whisper
- Nhận tham số từ C#, trả kết quả qua stdout

## 🎯 Công nghệ sử dụng

### .NET Side:
- **WPF** (.NET 6/7/8) - UI
- **MVVM Pattern** - Architecture
- **System.Diagnostics.Process** - Gọi Python
- **Serilog/NLog** - Logging
- **Newtonsoft.Json** - Parse kết quả từ Python

### Python Side:
- **FFmpeg** - Convert video sang WAV
- **Whisper** (OpenAI) - Transcribe audio
- **Python 3.9+** - Runtime

## 📂 Cấu trúc Solution đề xuất

```
VideoSubtitleGenerator/
│
├── src/
│   ├── VideoSubtitleGenerator.UI.Wpf/          # WPF Application
│   │   ├── Views/                               # XAML Views
│   │   ├── ViewModels/                          # ViewModels
│   │   ├── Commands/                            # RelayCommand, DelegateCommand
│   │   ├── Converters/                          # Value Converters
│   │   ├── Resources/                           # Styles, Templates
│   │   ├── App.xaml
│   │   └── MainWindow.xaml
│   │
│   ├── VideoSubtitleGenerator.Core/            # Core Business Logic
│   │   ├── Models/                              # Domain Models
│   │   │   ├── MediaFile.cs
│   │   │   ├── TranscriptionJob.cs
│   │   │   ├── JobStatus.cs (enum)
│   │   │   ├── TranscriptionResult.cs
│   │   │   └── AppSettings.cs
│   │   │
│   │   ├── Interfaces/                          # Contracts
│   │   │   ├── IPythonWorkerService.cs
│   │   │   ├── IJobQueueService.cs
│   │   │   ├── IJobOrchestrator.cs
│   │   │   ├── ISettingsService.cs
│   │   │   └── ILogService.cs
│   │   │
│   │   └── Services/                            # Core Services
│   │       ├── JobQueueService.cs
│   │       └── JobOrchestrator.cs
│   │
│   └── VideoSubtitleGenerator.Infrastructure/  # Infrastructure
│       ├── Services/
│       │   ├── PythonWorkerService.cs          # Implement IPythonWorkerService
│       │   ├── SettingsService.cs              # Read/Write JSON config
│       │   └── FileLogService.cs               # Logging implementation
│       │
│       └── Helpers/
│           └── ProcessRunner.cs                # Helper to run external process
│
├── python-worker/                               # Python Scripts
│   ├── venv/                                    # Virtual Environment (gitignored)
│   ├── process_media.py                         # Main script
│   ├── requirements.txt                         # Dependencies
│   └── README.md                                # Python setup guide
│
├── tests/                                       # Unit Tests
│   └── VideoSubtitleGenerator.Tests/
│
├── docs/                                        # Documentation
│   ├── architecture.md
│   ├── flow-diagram.md
│   └── deployment-guide.md
│
├── deployment/                                  # Deployment artifacts
│   ├── ffmpeg/                                  # Bundled FFmpeg (optional)
│   └── installer/                               # Setup project
│
└── VideoSubtitleGenerator.sln                  # Solution file
```

## 🔄 Luồng xử lý (Processing Flow)

### User Interaction:
1. User mở app WPF
2. Chọn nhiều file video (OpenFileDialog)
3. Chọn thư mục output
4. Cấu hình: Model, Language, Device, MaxParallelJobs
5. Click "Start Processing"

### Internal Processing:
1. **UI** → `JobQueueService.AddJobs(files)`
2. **JobQueueService** → Tạo `TranscriptionJob[]`, status = Pending
3. **JobOrchestrator** → 
   - Worker threads pull jobs từ queue
   - Call `IPythonWorkerService.RunAsync(job)`
4. **PythonWorkerService** →
   - Build command: `python process_media.py --input "..." --output-dir "..."`
   - Start Process
   - Capture stdout/stderr
   - Parse JSON result
5. **JobOrchestrator** → Update job status (Completed/Failed)
6. **UI** → Update ListView, show logs

## ⚙️ Configuration (appsettings.json)

```json
{
  "Python": {
    "PythonExePath": "python.exe",
    "ScriptPath": "python-worker\\process_media.py",
    "VenvPath": "python-worker\\venv"
  },
  "FFmpeg": {
    "UseBundled": true,
    "ExecutablePath": "deployment\\ffmpeg\\bin\\ffmpeg.exe"
  },
  "Whisper": {
    "DefaultModel": "small",
    "DefaultLanguage": "English",
    "DefaultDevice": "cpu",
    "AvailableModels": ["tiny", "base", "small", "medium", "large"]
  },
  "Processing": {
    "MaxParallelJobs": 2,
    "AutoDeleteWavFiles": true,
    "OutputFormat": "srt"
  },
  "Logging": {
    "LogFilePath": "logs\\app.log",
    "MinimumLevel": "Information"
  }
}
```

## 📦 Deployment Structure

```
VideoSubtitleGenerator_v1.0/
├── VideoSubtitleGenerator.UI.Wpf.exe
├── appsettings.json
├── VideoSubtitleGenerator.Core.dll
├── VideoSubtitleGenerator.Infrastructure.dll
├── ffmpeg/
│   └── bin/
│       └── ffmpeg.exe
├── python-worker/
│   ├── venv/
│   ├── process_media.py
│   └── requirements.txt
└── logs/
```

## 🎨 UI Components cần thiết

### MainWindow:
- **File Selection Panel**
  - Button: Add Files
  - Button: Remove Selected
  - Button: Clear All
  - TextBox: Output Directory
  - Button: Browse Output

- **File List (DataGrid/ListView)**
  - Columns: Filename, Path, Status, Start Time, End Time, Actions

- **Control Panel**
  - Button: Start Processing
  - Button: Pause All (optional)
  - Button: Cancel
  - ProgressBar: Overall progress

- **Settings Panel (Expander or Tab)**
  - ComboBox: Whisper Model
  - ComboBox: Language
  - ComboBox: Device (CPU/GPU)
  - NumericUpDown: Max Parallel Jobs

- **Log Viewer**
  - TextBox: Multi-line, read-only, auto-scroll

### SettingsWindow (optional):
- Advanced settings
- Path configurations
- Save/Load profiles

## 🚀 Roadmap phát triển

### Phase 1: Foundation (Week 1)
- [ ] Setup Solution structure
- [ ] Create Core models and interfaces
- [ ] Basic WPF UI skeleton

### Phase 2: Core Logic (Week 2)
- [ ] Implement JobQueueService
- [ ] Implement JobOrchestrator
- [ ] Unit tests for Core

### Phase 3: Infrastructure (Week 2-3)
- [ ] PythonWorkerService implementation
- [ ] SettingsService with JSON
- [ ] Logging service

### Phase 4: Python Worker (Week 3)
- [ ] process_media.py script
- [ ] FFmpeg integration
- [ ] Whisper integration
- [ ] Error handling & JSON output

### Phase 5: UI Implementation (Week 4)
- [ ] Complete MainWindow XAML
- [ ] ViewModels with INotifyPropertyChanged
- [ ] Commands binding
- [ ] Progress tracking

### Phase 6: Polish & Testing (Week 5)
- [ ] Error handling
- [ ] User feedback (notifications)
- [ ] Integration testing
- [ ] Performance optimization

### Phase 7: Deployment (Week 6)
- [ ] Bundle FFmpeg
- [ ] Python venv setup script
- [ ] Installer (WiX or ClickOnce)
- [ ] Documentation

## 📝 Notes
- Sử dụng async/await để tránh block UI thread
- Progress reporting qua IProgress<T> hoặc events
- Cancellation support qua CancellationToken
- Proper disposal of Process objects
- Validate Python/FFmpeg existence trước khi chạy
