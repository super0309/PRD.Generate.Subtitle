# Final Architecture Decision - Enhanced Sequential Processing

## 📋 Requirements Analysis

### User Requirements:
- **Files**: Có thể nhiều files (flexible)
- **Processing Mode**: 
  - ✅ Default: Sequential (từng file một)
  - ✅ Optional: Parallel (user confirm trước)
- **File Duration**: ~20 phút trung bình
- **Timeline**: Bình thường (không gấp)
- **Budget**: Nhỏ

### Decision: **Enhanced Option 1** (Process-per-job with Smart Features)

## 🏗️ Final Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    WPF Application (UI)                      │
│                                                               │
│  ┌──────────────┐    ┌────────────────┐   ┌──────────────┐ │
│  │  MainWindow  │───→│ MainViewModel  │──→│ Commands     │ │
│  │  (XAML)      │    │ (MVVM)         │   │ & Bindings   │ │
│  └──────────────┘    └────────┬───────┘   └──────────────┘ │
│                               │                              │
└───────────────────────────────┼──────────────────────────────┘
                                │
                    ┌───────────▼────────────┐
                    │   Core Layer           │
                    │                        │
                    │  ┌──────────────────┐  │
                    │  │ JobQueueService  │  │
                    │  │ - Add jobs       │  │
                    │  │ - Track status   │  │
                    │  └────────┬─────────┘  │
                    │           │            │
                    │  ┌────────▼─────────┐  │
                    │  │ JobOrchestrator  │  │
                    │  │ - Sequential     │  │
                    │  │ - Parallel (opt) │  │
                    │  └────────┬─────────┘  │
                    └───────────┼────────────┘
                                │
                    ┌───────────▼──────────────┐
                    │  Infrastructure Layer    │
                    │                          │
                    │  ┌────────────────────┐  │
                    │  │ PythonWorkerService│  │
                    │  │ - Launch process   │  │
                    │  │ - Parse output     │  │
                    │  │ - Handle errors    │  │
                    │  └────────┬───────────┘  │
                    └───────────┼──────────────┘
                                │ Process.Start()
                    ┌───────────▼──────────────┐
                    │   Python Worker          │
                    │   (process_media.py)     │
                    │                          │
                    │   ┌──────────────────┐   │
                    │   │  FFmpeg Convert  │   │
                    │   └────────┬─────────┘   │
                    │            │             │
                    │   ┌────────▼─────────┐   │
                    │   │ Whisper AI       │   │
                    │   │ Transcribe       │   │
                    │   └──────────────────┘   │
                    └──────────────────────────┘
```

## 🎯 Key Features

### 1. **Sequential Processing (Default)**
```csharp
public class JobOrchestrator : IJobOrchestrator
{
    private ProcessingMode _mode = ProcessingMode.Sequential;
    
    public async Task StartProcessingAsync(
        ProcessingMode mode, 
        CancellationToken ct)
    {
        _mode = mode;
        
        if (_mode == ProcessingMode.Sequential)
        {
            await ProcessSequentiallyAsync(ct);
        }
        else
        {
            await ProcessInParallelAsync(ct);
        }
    }
    
    private async Task ProcessSequentiallyAsync(CancellationToken ct)
    {
        var jobs = _jobQueue.GetPendingJobs();
        
        foreach (var job in jobs)
        {
            if (ct.IsCancellationRequested) break;
            
            await ProcessSingleJobAsync(job, ct);
        }
    }
    
    private async Task ProcessInParallelAsync(CancellationToken ct)
    {
        var jobs = _jobQueue.GetPendingJobs();
        var maxParallel = _settings.MaxParallelJobs;
        
        var semaphore = new SemaphoreSlim(maxParallel);
        var tasks = jobs.Select(async job =>
        {
            await semaphore.WaitAsync(ct);
            try
            {
                await ProcessSingleJobAsync(job, ct);
            }
            finally
            {
                semaphore.Release();
            }
        });
        
        await Task.WhenAll(tasks);
    }
}
```

### 2. **User Confirmation Dialog**
```csharp
public class MainViewModel : ViewModelBase
{
    public ICommand StartProcessingCommand { get; }
    
    private async void StartProcessing()
    {
        if (Jobs.Count == 0) return;
        
        // Show processing mode dialog
        var dialog = new ProcessingModeDialog
        {
            TotalFiles = Jobs.Count,
            EstimatedTimeSequential = EstimateTime(ProcessingMode.Sequential),
            EstimatedTimeParallel = EstimateTime(ProcessingMode.Parallel),
            RecommendedMode = GetRecommendedMode()
        };
        
        if (dialog.ShowDialog() == true)
        {
            var mode = dialog.SelectedMode;
            await _orchestrator.StartProcessingAsync(mode, _cts.Token);
        }
    }
    
    private ProcessingMode GetRecommendedMode()
    {
        // Nếu <= 3 files hoặc file dài → Sequential
        if (Jobs.Count <= 3 || AverageFileDuration > TimeSpan.FromMinutes(15))
        {
            return ProcessingMode.Sequential;
        }
        
        // Nếu có nhiều files ngắn → Parallel
        return ProcessingMode.Parallel;
    }
}
```

### 3. **Smart Progress Tracking**
```csharp
public class TranscriptionJob
{
    public Guid Id { get; set; }
    public string InputFilePath { get; set; }
    public JobStatus Status { get; set; }
    public int Progress { get; set; }  // 0-100
    
    // Enhanced tracking
    public ProcessingPhase CurrentPhase { get; set; }
    public TimeSpan? EstimatedTimeRemaining { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
}

public enum ProcessingPhase
{
    Queued,
    Converting,      // FFmpeg đang convert
    Transcribing,    // Whisper đang transcribe
    Finalizing,      // Đang lưu file
    Completed,
    Failed
}
```

### 4. **Enhanced Python Worker với Progress**
```python
# process_media.py (Enhanced)

import sys
import json
import os
from pathlib import Path
import subprocess

def log_progress(phase, percent, message):
    """Send progress update to C#"""
    progress = {
        'phase': phase,
        'percent': percent,
        'message': message
    }
    print(f"PROGRESS:{json.dumps(progress)}", file=sys.stderr, flush=True)

def convert_to_wav(input_file, output_dir):
    """Convert video to WAV with progress tracking"""
    try:
        log_progress('Converting', 0, 'Starting FFmpeg conversion...')
        
        input_path = Path(input_file)
        wav_filename = input_path.stem + '.wav'
        wav_path = os.path.join(output_dir, wav_filename)
        
        ffmpeg_cmd = [
            'ffmpeg',
            '-y',
            '-i', input_file,
            '-ar', '16000',
            '-ac', '1',
            '-c:a', 'pcm_s16le',
            '-progress', 'pipe:2',  # Output progress to stderr
            wav_path
        ]
        
        process = subprocess.Popen(
            ffmpeg_cmd,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            text=True,
            bufsize=1
        )
        
        # Parse FFmpeg progress
        for line in process.stderr:
            if line.startswith('out_time_ms='):
                # Extract progress percentage
                # (requires knowing input duration)
                pass
            elif 'time=' in line:
                log_progress('Converting', 50, 'Converting audio...')
        
        process.wait()
        
        if process.returncode != 0:
            log_progress('Converting', 0, 'FFmpeg conversion failed')
            return None
        
        log_progress('Converting', 100, 'Conversion complete')
        return wav_path
        
    except Exception as e:
        log_progress('Converting', 0, f'Error: {str(e)}')
        return None

def transcribe_audio(wav_file, args):
    """Transcribe with progress updates"""
    try:
        log_progress('Transcribing', 0, 'Loading Whisper model...')
        
        # Import whisper (will show loading progress)
        import whisper
        
        log_progress('Transcribing', 20, 'Model loaded, starting transcription...')
        
        model = whisper.load_model(args.model, device=args.device)
        
        log_progress('Transcribing', 40, 'Transcribing audio...')
        
        result = model.transcribe(
            wav_file,
            language=args.language,
            task=args.task,
            fp16=(args.fp16.lower() == 'true')
        )
        
        log_progress('Transcribing', 80, 'Saving subtitle file...')
        
        # Save subtitle
        from whisper.utils import get_writer
        writer = get_writer(args.output_format, args.output_dir)
        writer(result, wav_file)
        
        log_progress('Transcribing', 100, 'Transcription complete')
        
        # Find subtitle file
        wav_path = Path(wav_file)
        subtitle_file = os.path.join(
            args.output_dir,
            wav_path.stem + '.' + args.output_format
        )
        
        return subtitle_file
        
    except Exception as e:
        log_progress('Transcribing', 0, f'Error: {str(e)}')
        return None

def main():
    args = parse_arguments()
    
    log_progress('Queued', 0, 'Starting job...')
    
    # Validate
    if not validate_inputs(args):
        output_error("Invalid inputs")
        sys.exit(1)
    
    # Convert
    wav_file = convert_to_wav(args.input, args.output_dir)
    if not wav_file:
        output_error("FFmpeg conversion failed")
        sys.exit(1)
    
    # Transcribe
    subtitle_file = transcribe_audio(wav_file, args)
    if not subtitle_file:
        output_error("Whisper transcription failed")
        sys.exit(1)
    
    # Cleanup WAV if needed
    if not args.keep_wav:
        try:
            os.remove(wav_file)
        except:
            pass
    
    # Output success
    output_success(wav_file, subtitle_file, args)
    sys.exit(0)

if __name__ == '__main__':
    main()
```

### 5. **C# Progress Parser**
```csharp
public class PythonWorkerService : IPythonWorkerService
{
    public async Task<TranscriptionResult> ProcessAsync(
        TranscriptionJob job,
        IProgress<JobProgress> progress,
        CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = _pythonPath,
            Arguments = BuildCommandArgs(job),
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        
        using var process = new Process { StartInfo = startInfo };
        
        var outputBuilder = new StringBuilder();
        
        // Parse stderr for progress updates
        process.ErrorDataReceived += (s, e) =>
        {
            if (e.Data == null) return;
            
            if (e.Data.StartsWith("PROGRESS:"))
            {
                var json = e.Data.Substring(9);
                var progressData = JsonSerializer.Deserialize<JobProgress>(json);
                
                // Update job status
                job.CurrentPhase = progressData.Phase;
                job.Progress = progressData.Percent;
                
                // Report to UI
                progress?.Report(progressData);
            }
            else
            {
                _logger.LogDebug(e.Data);
            }
        };
        
        // Capture stdout for final result
        process.OutputDataReceived += (s, e) =>
        {
            if (e.Data != null)
            {
                outputBuilder.AppendLine(e.Data);
            }
        };
        
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        
        await process.WaitForExitAsync(cancellationToken);
        
        // Parse result
        var result = ParseResult(outputBuilder.ToString());
        return result;
    }
}

public class JobProgress
{
    public ProcessingPhase Phase { get; set; }
    public int Percent { get; set; }
    public string Message { get; set; }
}
```

## 📊 UI Design

### MainWindow Layout:
```xml
<Window>
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>      <!-- Toolbar -->
            <RowDefinition Height="*"/>         <!-- Job List -->
            <RowDefinition Height="Auto"/>      <!-- Progress Summary -->
            <RowDefinition Height="200"/>       <!-- Log Viewer -->
        </Grid.RowDefinitions>
        
        <!-- Toolbar -->
        <StackPanel Grid.Row="0" Orientation="Horizontal">
            <Button Command="{Binding AddFilesCommand}">
                Add Files
            </Button>
            <Button Command="{Binding StartProcessingCommand}">
                Start Processing
            </Button>
            <Button Command="{Binding CancelCommand}">
                Cancel
            </Button>
            <Separator/>
            <Button Command="{Binding OpenSettingsCommand}">
                Settings
            </Button>
        </StackPanel>
        
        <!-- Job List -->
        <DataGrid Grid.Row="1" 
                  ItemsSource="{Binding Jobs}"
                  AutoGenerateColumns="False">
            <DataGrid.Columns>
                <DataGridTextColumn Header="File" 
                                    Binding="{Binding FileName}"/>
                <DataGridTextColumn Header="Status" 
                                    Binding="{Binding StatusText}"/>
                <DataGridTemplateColumn Header="Progress">
                    <DataGridTemplateColumn.CellTemplate>
                        <DataTemplate>
                            <StackPanel>
                                <ProgressBar Value="{Binding Progress}" 
                                            Maximum="100"
                                            Height="20"/>
                                <TextBlock Text="{Binding ProgressText}"
                                          FontSize="10"
                                          HorizontalAlignment="Center"/>
                            </StackPanel>
                        </DataTemplate>
                    </DataGridTemplateColumn.CellTemplate>
                </DataGridTemplateColumn>
                <DataGridTextColumn Header="Time" 
                                    Binding="{Binding ElapsedTime}"/>
            </DataGrid.Columns>
        </DataGrid>
        
        <!-- Overall Progress -->
        <StackPanel Grid.Row="2" Margin="10">
            <TextBlock>
                <Run Text="Overall: "/>
                <Run Text="{Binding CompletedCount}"/>
                <Run Text=" / "/>
                <Run Text="{Binding TotalCount}"/>
                <Run Text=" completed"/>
            </TextBlock>
            <ProgressBar Value="{Binding OverallProgress}" 
                        Maximum="100"
                        Height="30"/>
            <TextBlock Text="{Binding EstimatedTimeRemaining}"
                      FontSize="12"
                      Foreground="Gray"/>
        </StackPanel>
        
        <!-- Log Viewer -->
        <TextBox Grid.Row="3" 
                 Text="{Binding LogText}"
                 IsReadOnly="True"
                 VerticalScrollBarVisibility="Auto"
                 FontFamily="Consolas"/>
    </Grid>
</Window>
```

### ProcessingModeDialog:
```xml
<Window Title="Choose Processing Mode" Width="500" Height="350">
    <StackPanel Margin="20">
        <TextBlock FontSize="16" FontWeight="Bold">
            Choose how to process files
        </TextBlock>
        
        <Border BorderBrush="Gray" BorderThickness="1" 
                Padding="10" Margin="0,20,0,0">
            <StackPanel>
                <RadioButton GroupName="Mode" 
                             IsChecked="True"
                             Content="Sequential (Recommended)"/>
                <TextBlock Margin="25,5,0,0" FontSize="12" 
                          Foreground="Gray">
                    • Process one file at a time
                </TextBlock>
                <TextBlock Margin="25,0,0,0" FontSize="12" 
                          Foreground="Gray">
                    • Lower memory usage
                </TextBlock>
                <TextBlock Margin="25,0,0,0" FontSize="12" 
                          Foreground="Gray">
                    • Estimated time: <Run Text="{Binding EstimatedTimeSequential}"/>
                </TextBlock>
            </StackPanel>
        </Border>
        
        <Border BorderBrush="Gray" BorderThickness="1" 
                Padding="10" Margin="0,10,0,0">
            <StackPanel>
                <RadioButton GroupName="Mode" 
                             Content="Parallel"/>
                <TextBlock Margin="25,5,0,0" FontSize="12" 
                          Foreground="Gray">
                    • Process multiple files simultaneously
                </TextBlock>
                <TextBlock Margin="25,0,0,0" FontSize="12" 
                          Foreground="Gray">
                    • Higher memory usage (up to 2GB per job)
                </TextBlock>
                <TextBlock Margin="25,0,0,0" FontSize="12" 
                          Foreground="Gray">
                    • Estimated time: <Run Text="{Binding EstimatedTimeParallel}"/>
                </TextBlock>
                <StackPanel Orientation="Horizontal" Margin="25,5,0,0">
                    <TextBlock Text="Max parallel jobs:" 
                              VerticalAlignment="Center"/>
                    <TextBox Text="{Binding MaxParallelJobs}" 
                            Width="50" Margin="5,0,0,0"/>
                </StackPanel>
            </StackPanel>
        </Border>
        
        <StackPanel Orientation="Horizontal" 
                   HorizontalAlignment="Right" 
                   Margin="0,20,0,0">
            <Button Content="Start" 
                   IsDefault="True" 
                   Width="80" 
                   Margin="0,0,10,0"
                   Click="StartButton_Click"/>
            <Button Content="Cancel" 
                   IsCancel="True" 
                   Width="80"
                   Click="CancelButton_Click"/>
        </StackPanel>
    </StackPanel>
</Window>
```

## 🎯 Advantages của phương án này

### 1. **Simplicity**
- ✅ Dễ implement (2-3 tuần)
- ✅ Ít dependencies
- ✅ Dễ debug
- ✅ Dễ maintain

### 2. **User Control**
- ✅ Default sequential → safe & stable
- ✅ Optional parallel → power users
- ✅ Clear estimation → informed decision

### 3. **Performance**
```
20 phút video:
- Model load: ~10s (8% overhead)
- FFmpeg convert: ~30s
- Whisper transcribe: ~6 phút
- Total: ~7 phút/file

10 files sequential: ~70 phút
10 files parallel (2 jobs): ~35 phút

→ Sequential acceptable cho budget nhỏ
→ Parallel available nếu cần
```

### 4. **Resource Friendly**
- ✅ Sequential: 500MB-1GB RAM
- ✅ Parallel (2 jobs): 1-2GB RAM
- ✅ User có thể chọn dựa trên máy của mình

### 5. **Progress Transparency**
- ✅ Phase-by-phase tracking
- ✅ Real-time updates
- ✅ ETA calculation
- ✅ Detailed logs

## 📋 Implementation Priority

### Phase 1: Core (Week 1)
- [ ] Solution structure
- [ ] Core models
- [ ] JobQueueService
- [ ] Sequential JobOrchestrator
- [ ] Basic PythonWorkerService

### Phase 2: Python Worker (Week 1-2)
- [ ] process_media.py với FFmpeg
- [ ] Whisper integration
- [ ] Progress reporting
- [ ] Error handling

### Phase 3: UI (Week 2)
- [ ] MainWindow XAML
- [ ] MainViewModel
- [ ] Job list binding
- [ ] Commands

### Phase 4: Enhancements (Week 2-3)
- [ ] ProcessingModeDialog
- [ ] Parallel processing option
- [ ] Progress parsing
- [ ] Settings panel

### Phase 5: Polish (Week 3)
- [ ] Error handling
- [ ] Retry logic
- [ ] Logging
- [ ] User feedback

### Phase 6: Deployment (Week 3-4)
- [ ] Bundle Python portable
- [ ] Bundle FFmpeg
- [ ] Create installer
- [ ] Documentation

## 🎉 Summary

Phương án này:
- ✅ **Phù hợp budget nhỏ** (simple implementation)
- ✅ **Đáp ứng use case** (sequential default, parallel optional)
- ✅ **Tối ưu cho files 20 phút** (overhead chấp nhận được)
- ✅ **User-friendly** (clear options, good feedback)
- ✅ **Maintainable** (clean architecture)
- ✅ **Scalable** (có thể upgrade lên Option 2 sau)

**Timeline**: 3-4 tuần implementation + 1 tuần testing = **~1 tháng total**
