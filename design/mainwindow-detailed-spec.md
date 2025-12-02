# MainWindow - Detailed Design Specification

## 📋 Overview
MainWindow là màn hình chính của ứng dụng, chiếm 90% thời gian sử dụng. User sẽ add files, theo dõi progress, xem logs và điều khiển processing tại đây.

---

## 🏗️ Layout Structure

```
MainWindow (Window)
│
├─ Root Grid (Grid with 4 rows)
│   │
│   ├─ Row 0: Toolbar Section (Auto height)
│   │   └─ ToolbarPanel (StackPanel/Toolbar)
│   │
│   ├─ Row 1: Job List Section (Star height - main area)
│   │   └─ JobListContainer (Border/GroupBox)
│   │       └─ JobsDataGrid (DataGrid)
│   │
│   ├─ Row 2: Progress Summary Section (Auto height)
│   │   └─ ProgressSummaryPanel (Border/GroupBox)
│   │       └─ Overall progress controls
│   │
│   └─ Row 3: Log Viewer Section (Fixed ~200px)
│       └─ LogViewerContainer (Border/GroupBox)
│           └─ LogTextBox (TextBox)
```

---

## 🎯 Section 1: TOOLBAR (Row 0)

### Container:
- **Control**: `StackPanel` hoặc `ToolBar`
- **Name**: `ToolbarPanel`
- **Orientation**: Horizontal
- **Height**: Auto (khoảng 40-50px)
- **Padding**: 5,5,5,5
- **Background**: Light gray hoặc gradient

### Controls trong Toolbar:

#### 1.1 Button: Add Files
```
Control Type: Button
Name: btnAddFiles
Content: "➕ Add Files" hoặc Icon + Text
Width: 100-120
Height: 32
Margin: 5,0,5,0
Tooltip: "Add video files to process (Ctrl+O)"
Icon: Folder/Plus icon
Command: {Binding AddFilesCommand}
IsEnabled: {Binding IsNotProcessing}
```

#### 1.2 Button: Remove Selected
```
Control Type: Button
Name: btnRemoveSelected
Content: "🗑️ Remove" hoặc Icon only
Width: 80-100
Height: 32
Margin: 0,0,5,0
Tooltip: "Remove selected files"
Icon: Delete/Trash icon
Command: {Binding RemoveSelectedCommand}
IsEnabled: {Binding HasSelectedJobs}
```

#### 1.3 Separator
```
Control Type: Separator
Orientation: Vertical
Margin: 5,5,5,5
```

#### 1.4 Button: Start Processing
```
Control Type: Button
Name: btnStartProcessing
Content: "▶️ Start Processing" hoặc Icon + Text
Width: 130-150
Height: 32
Margin: 0,0,5,0
Tooltip: "Start processing all files (Ctrl+S)"
Icon: Play icon
Background: Green/Primary color
Foreground: White
FontWeight: Bold
Command: {Binding StartProcessingCommand}
IsEnabled: {Binding CanStartProcessing}
```

#### 1.5 Button: Pause
```
Control Type: Button
Name: btnPause
Content: "⏸️ Pause"
Width: 80
Height: 32
Margin: 0,0,5,0
Tooltip: "Pause current processing"
Icon: Pause icon
Command: {Binding PauseProcessingCommand}
IsEnabled: {Binding IsProcessing}
Visibility: {Binding IsProcessing, Converter=BoolToVisibility}
```

#### 1.6 Button: Stop/Cancel
```
Control Type: Button
Name: btnStop
Content: "⏹️ Stop"
Width: 80
Height: 32
Margin: 0,0,5,0
Tooltip: "Stop all processing"
Icon: Stop icon
Background: Orange/Warning color
Command: {Binding StopProcessingCommand}
IsEnabled: {Binding IsProcessing}
Visibility: {Binding IsProcessing, Converter=BoolToVisibility}
```

#### 1.7 Separator
```
Control Type: Separator
Orientation: Vertical
Margin: 5,5,5,5
```

#### 1.8 Button: Browse Output Folder
```
Control Type: Button
Name: btnBrowseOutput
Content: "📁 Output Folder..."
Width: 130
Height: 32
Margin: 0,0,5,0
Tooltip: "Select output folder for subtitles"
Icon: Folder icon
Command: {Binding BrowseOutputFolderCommand}
IsEnabled: {Binding IsNotProcessing}
```

#### 1.9 TextBox: Output Path Display (Optional)
```
Control Type: TextBox
Name: txtOutputPath
Width: 200-300
Height: 32
Margin: 0,0,10,0
IsReadOnly: True
Text: {Binding OutputDirectory}
VerticalContentAlignment: Center
Tooltip: {Binding OutputDirectory}
Background: #F5F5F5
```

#### 1.10 Spacer (Push right-aligned items to right)
```
Control Type: FrameworkElement or Border
HorizontalAlignment: Stretch
Width: Star (*)
```

#### 1.11 Button: Settings
```
Control Type: Button
Name: btnSettings
Content: "⚙️ Settings"
Width: 90
Height: 32
Margin: 0,0,5,0
Tooltip: "Open settings (Ctrl+,)"
Icon: Gear/Settings icon
Command: {Binding OpenSettingsCommand}
```

#### 1.12 Button: About/Help (Optional)
```
Control Type: Button
Name: btnAbout
Content: "ℹ️"
Width: 32
Height: 32
Margin: 0,0,5,0
Tooltip: "About this application"
Icon: Info icon
Command: {Binding OpenAboutCommand}
```

---

## 🎯 Section 2: JOB LIST (Row 1)

### Container:
```
Control Type: GroupBox hoặc Border
Name: JobListContainer
Header: "📁 Files & Processing Queue" (if GroupBox)
Margin: 5,5,5,5
Padding: 10
BorderBrush: #CCCCCC
BorderThickness: 1
```

### Main Control: DataGrid

```
Control Type: DataGrid
Name: JobsDataGrid
ItemsSource: {Binding Jobs}
SelectedItem: {Binding SelectedJob}
SelectionMode: Extended (cho multi-select)
AutoGenerateColumns: False
CanUserAddRows: False
CanUserDeleteRows: False
CanUserReorderColumns: True
CanUserResizeColumns: True
CanUserSortColumns: True
GridLinesVisibility: Horizontal
HeadersVisibility: All
AlternatingRowBackground: #F9F9F9
RowHeight: 60 (để hiển thị 2 dòng progress)
```

### DataGrid Columns:

#### Column 0: Checkbox (Select)
```
Column Type: DataGridCheckBoxColumn
Header: ""
Width: 40
Binding: {Binding IsSelected, Mode=TwoWay}
CanUserResize: False
CanUserSort: False
```

#### Column 1: File Icon + Name
```
Column Type: DataGridTemplateColumn
Header: "Filename"
Width: 250-300 (hoặc *)
MinWidth: 200
SortMemberPath: FileName

CellTemplate:
  StackPanel (Orientation: Horizontal)
    ├─ Image (Icon)
    │   Source: {Binding FileIcon}
    │   Width: 24, Height: 24
    │   Margin: 0,0,8,0
    │
    └─ StackPanel (Orientation: Vertical)
        ├─ TextBlock (FileName)
        │   Text: {Binding FileName}
        │   FontWeight: SemiBold
        │   FontSize: 12
        │   TextTrimming: CharacterEllipsis
        │   ToolTip: {Binding FullPath}
        │
        └─ TextBlock (File Info - Optional)
            Text: {Binding FileSize} + " | " + {Binding Duration}
            FontSize: 10
            Foreground: Gray
```

#### Column 2: Status
```
Column Type: DataGridTemplateColumn
Header: "Status"
Width: 120
SortMemberPath: Status

CellTemplate:
  StackPanel (Orientation: Horizontal)
    ├─ Ellipse (Status Indicator)
    │   Width: 12, Height: 12
    │   Fill: {Binding StatusColor}
    │   Margin: 0,0,8,0
    │   VerticalAlignment: Center
    │
    └─ TextBlock
        Text: {Binding StatusText}
        FontSize: 12
        FontWeight: Medium
        Foreground: {Binding StatusColor}
        VerticalAlignment: Center
```

**Status Colors:**
- Queued: #757575 (Gray)
- Converting: #2196F3 (Blue)
- Transcribing: #9C27B0 (Purple)
- Completed: #4CAF50 (Green)
- Failed: #F44336 (Red)
- Canceled: #FF9800 (Orange)

#### Column 3: Progress
```
Column Type: DataGridTemplateColumn
Header: "Progress"
Width: 200-250
MinWidth: 150
CanUserSort: True
SortMemberPath: Progress

CellTemplate:
  StackPanel (Orientation: Vertical)
    ├─ ProgressBar
    │   Value: {Binding Progress}
    │   Minimum: 0
    │   Maximum: 100
    │   Height: 20
    │   Margin: 0,5,0,2
    │   Foreground: {Binding ProgressBarColor}
    │   IsIndeterminate: {Binding IsIndeterminate}
    │
    ├─ TextBlock (Progress Text)
    │   Text: {Binding ProgressText}
    │   FontSize: 11
    │   HorizontalAlignment: Center
    │   Margin: 0,2,0,2
    │   Example: "75%" hoặc "45% - Converting"
    │
    └─ TextBlock (Phase Text)
        Text: {Binding PhaseText}
        FontSize: 9
        Foreground: Gray
        HorizontalAlignment: Center
        Example: "Phase: FFmpeg Conversion"
```

#### Column 4: Time
```
Column Type: DataGridTemplateColumn
Header: "Time"
Width: 80-100
SortMemberPath: ElapsedTime

CellTemplate:
  StackPanel (Orientation: Vertical)
    ├─ TextBlock (Elapsed Time)
    │   Text: {Binding ElapsedTimeText}
    │   FontSize: 12
    │   FontFamily: Consolas (monospace)
    │   HorizontalAlignment: Center
    │   Example: "05:34"
    │
    └─ TextBlock (ETA - Optional)
        Text: {Binding EstimatedTimeRemaining}
        FontSize: 9
        Foreground: Gray
        HorizontalAlignment: Center
        Example: "~02:15 left"
        Visibility: {Binding IsProcessing, Converter=BoolToVisibility}
```

#### Column 5: Actions Menu
```
Column Type: DataGridTemplateColumn
Header: ""
Width: 40
CanUserResize: False
CanUserSort: False

CellTemplate:
  Button (Menu Button)
    Content: "⋮" (vertical ellipsis)
    Width: 30, Height: 30
    Style: Flat button style
    ContextMenu:
      ├─ MenuItem: "View Details"
      │   Command: {Binding ViewDetailsCommand}
      │   Icon: Info icon
      │
      ├─ MenuItem: "Open Output Folder"
      │   Command: {Binding OpenOutputFolderCommand}
      │   Icon: Folder icon
      │   IsEnabled: {Binding IsCompleted}
      │
      ├─ MenuItem: "Open Subtitle File"
      │   Command: {Binding OpenSubtitleFileCommand}
      │   Icon: Document icon
      │   IsEnabled: {Binding IsCompleted}
      │
      ├─ Separator
      │
      ├─ MenuItem: "Retry"
      │   Command: {Binding RetryJobCommand}
      │   Icon: Refresh icon
      │   IsEnabled: {Binding IsFailed}
      │
      ├─ MenuItem: "Cancel"
      │   Command: {Binding CancelJobCommand}
      │   Icon: Stop icon
      │   IsEnabled: {Binding IsProcessing}
      │
      ├─ Separator
      │
      └─ MenuItem: "Remove"
          Command: {Binding RemoveJobCommand}
          Icon: Delete icon
          IsEnabled: {Binding CanRemove}
```

### DataGrid Context Menu (Right-click on row):
```
ContextMenu:
  ├─ MenuItem: "Open File Location"
  │   Command: {Binding OpenFileLocationCommand}
  │
  ├─ MenuItem: "Copy File Path"
  │   Command: {Binding CopyFilePathCommand}
  │
  ├─ Separator
  │
  ├─ MenuItem: "View Details"
  │   Command: {Binding ViewDetailsCommand}
  │
  ├─ Separator
  │
  ├─ MenuItem: "Retry Selected"
  │   Command: {Binding RetrySelectedCommand}
  │
  ├─ MenuItem: "Remove Selected"
  │   Command: {Binding RemoveSelectedCommand}
  │
  └─ MenuItem: "Clear All"
      Command: {Binding ClearAllCommand}
```

### Empty State (When no files):
```
Overlay hoặc TextBlock trong DataGrid:
  TextBlock
    Text: "No files added yet.\nClick 'Add Files' or drag & drop video files here."
    FontSize: 14
    Foreground: Gray
    HorizontalAlignment: Center
    VerticalAlignment: Center
    TextAlignment: Center
    Visibility: {Binding HasNoJobs, Converter=BoolToVisibility}
```

---

## 🎯 Section 3: PROGRESS SUMMARY (Row 2)

### Container:
```
Control Type: Border hoặc GroupBox
Name: ProgressSummaryPanel
Header: "📊 Overall Progress" (if GroupBox)
Height: Auto (khoảng 100-120px)
Margin: 5,5,5,5
Padding: 10
BorderBrush: #CCCCCC
BorderThickness: 1
Background: #F5F5F5 (light background)
```

### Layout: Grid (3 rows)
```
Grid.RowDefinitions:
  Row 0: Auto (Summary text)
  Row 1: Auto (Progress bar)
  Row 2: Auto (Stats)
```

#### Row 0: Summary Text
```
Control Type: TextBlock
Name: txtOverallSummary
Margin: 0,0,0,8
FontSize: 13
FontWeight: Medium

Content Template:
  TextBlock:
    <Run Text="Processing: "/>
    <Run Text="{Binding ActiveJobsCount}" FontWeight="Bold" Foreground="#2196F3"/>
    <Run Text=" active | Completed: "/>
    <Run Text="{Binding CompletedJobsCount}" FontWeight="Bold" Foreground="#4CAF50"/>
    <Run Text=" / "/>
    <Run Text="{Binding TotalJobsCount}" FontWeight="Bold"/>
    <Run Text=" files"/>
    
    Example output: "Processing: 2 active | Completed: 5 / 10 files"
```

#### Row 1: Overall ProgressBar
```
Control Type: ProgressBar
Name: pbOverallProgress
Height: 30
Margin: 0,0,0,8
Minimum: 0
Maximum: 100
Value: {Binding OverallProgressPercentage}
Foreground: #2196F3 (Blue)
IsIndeterminate: False

Style: Modern flat style with rounded corners
```

#### Row 1 (Overlay): Progress Percentage Text
```
Control Type: TextBlock (overlay on ProgressBar)
Text: {Binding OverallProgressPercentage, StringFormat='{}{0}%'}
HorizontalAlignment: Center
VerticalAlignment: Center
FontSize: 14
FontWeight: Bold
Foreground: White (or contrasting color)
```

#### Row 2: Statistics Grid
```
Grid with 3 columns (equally divided):
  
  Column 0: Total Time
    StackPanel (Orientation: Horizontal)
      ├─ TextBlock (Icon)
      │   Text: "⏱️"
      │   Margin: 0,0,5,0
      │
      └─ StackPanel (Orientation: Vertical)
          ├─ TextBlock (Label)
          │   Text: "Total Time"
          │   FontSize: 10
          │   Foreground: Gray
          │
          └─ TextBlock (Value)
              Text: {Binding TotalElapsedTime}
              FontSize: 12
              FontWeight: SemiBold
              Example: "25:34"
  
  Column 1: Estimated Remaining
    StackPanel (Orientation: Horizontal)
      ├─ TextBlock (Icon)
      │   Text: "⏳"
      │   Margin: 0,0,5,0
      │
      └─ StackPanel (Orientation: Vertical)
          ├─ TextBlock (Label)
          │   Text: "Est. Remaining"
          │   FontSize: 10
          │   Foreground: Gray
          │
          └─ TextBlock (Value)
              Text: {Binding EstimatedTimeRemaining}
              FontSize: 12
              FontWeight: SemiBold
              Foreground: #FF9800 (Orange)
              Example: "~35 minutes"
  
  Column 2: Processing Speed
    StackPanel (Orientation: Horizontal)
      ├─ TextBlock (Icon)
      │   Text: "🚀"
      │   Margin: 0,0,5,0
      │
      └─ StackPanel (Orientation: Vertical)
          ├─ TextBlock (Label)
          │   Text: "Speed"
          │   FontSize: 10
          │   Foreground: Gray
          │
          └─ TextBlock (Value)
              Text: {Binding ProcessingSpeed}
              FontSize: 12
              FontWeight: SemiBold
              Example: "0.4x realtime"
              ToolTip: "Whisper processes audio at 0.4x the video duration"
```

---

## 🎯 Section 4: LOG VIEWER (Row 3)

### Container:
```
Control Type: GroupBox hoặc Border
Name: LogViewerContainer
Header: "📝 Processing Log" (if GroupBox)
Height: 200 (fixed hoặc có thể resize)
Margin: 5,5,5,5
Padding: 5
```

### Header Bar (inside container):
```
StackPanel (Orientation: Horizontal, HorizontalAlignment: Right)
  ├─ TextBlock
  │   Text: "📝 Processing Log"
  │   FontSize: 13
  │   FontWeight: SemiBold
  │   VerticalAlignment: Center
  │   Margin: 0,0,10,0
  │
  ├─ Button: Clear Log
  │   Content: "Clear"
  │   Width: 60
  │   Height: 24
  │   Margin: 0,0,5,0
  │   Command: {Binding ClearLogCommand}
  │
  └─ Button: Save Log
      Content: "Save..."
      Width: 70
      Height: 24
      Command: {Binding SaveLogCommand}
```

### Main Control: TextBox (Log)
```
Control Type: TextBox
Name: txtLog
AcceptsReturn: True
IsReadOnly: True
VerticalScrollBarVisibility: Auto
HorizontalScrollBarVisibility: Auto
TextWrapping: NoWrap
Text: {Binding LogText}
FontFamily: Consolas (monospace)
FontSize: 10
Background: #1E1E1E (dark theme) hoặc White (light theme)
Foreground: #D4D4D4 (light gray for dark theme) hoặc Black
Padding: 5

Auto-scroll to bottom: Yes (khi có log mới)
```

### Log Format Examples:
```
[INFO ] 14:23:45 - Application started
[INFO ] 14:23:46 - Added 10 files to queue
[INFO ] 14:23:47 - Starting batch processing (Sequential mode)
[DEBUG] 14:23:48 - Job 1/10: Module.9.3.mpeg
[DEBUG] 14:23:49 - FFmpeg: Converting to WAV (16kHz, mono)...
[DEBUG] 14:24:12 - FFmpeg: Conversion complete (30.5s)
[INFO ] 14:24:13 - Loading Whisper model (small)...
[DEBUG] 14:24:18 - Model loaded successfully
[INFO ] 14:24:19 - Transcribing audio...
[DEBUG] 14:25:00 - Processing segment 1/10
[WARN ] 14:25:15 - Low confidence detected: 0.65
[INFO ] 14:25:34 - Subtitle saved: C:\Output\Module.9.3.srt
[SUCCESS] 14:25:34 - Job completed successfully! (Duration: 1:46)
[ERROR] 14:26:01 - Job 5/10 failed: Demo.mov - File is corrupted
[INFO ] 14:30:00 - Batch processing completed: 9/10 successful
```

### Log Color Coding (via styled TextBlock hoặc RichTextBox):
```
[INFO ]    - White/Black (default)
[DEBUG]    - Gray
[WARN ]    - Orange/Yellow
[ERROR]    - Red
[SUCCESS]  - Green
```

---

## 🎨 Visual States & Animations

### State 1: Idle (No jobs, not processing)
```
btnAddFiles: Enabled, highlighted
btnStartProcessing: Disabled
btnPause, btnStop: Hidden
JobsDataGrid: Empty state message visible
ProgressSummaryPanel: Hidden or shows "No jobs"
```

### State 2: Jobs Added (Ready to start)
```
btnAddFiles: Enabled
btnStartProcessing: Enabled, highlighted (green)
btnRemoveSelected: Enabled if selection
JobsDataGrid: Shows jobs with "Queued" status
ProgressSummaryPanel: Shows "0/N files"
```

### State 3: Processing (Running)
```
btnAddFiles: Disabled
btnStartProcessing: Hidden or Disabled
btnPause: Visible, Enabled
btnStop: Visible, Enabled
JobsDataGrid: Shows jobs with various statuses
  - Active jobs: Progress bar animating
  - Completed jobs: Green checkmark
ProgressSummaryPanel: Updating in real-time
txtLog: Auto-scrolling with new messages
```

### State 4: Paused
```
btnPause: Changes to "Resume" button
btnStop: Enabled
Progress bars: Frozen at current value
Log: Shows "Processing paused by user"
```

### State 5: Completed
```
btnStartProcessing: Text changes to "Process Again" (if failed jobs exist)
All jobs: Either Completed or Failed
ProgressSummaryPanel: Shows final summary
Log: Shows completion message
Optional: Show notification/dialog with summary
```

### Animations:
```
1. ProgressBar: Smooth value change (300ms)
2. Status icon: Pulse animation when active
3. New log entry: Fade in (200ms)
4. Row selection: Highlight transition (150ms)
5. Button hover: Scale 1.05 + shadow
```

---

## 📝 Data Bindings Checklist

### MainViewModel Properties:
```
ObservableCollection<JobViewModel> Jobs
JobViewModel SelectedJob
bool IsProcessing
bool IsNotProcessing => !IsProcessing
bool HasJobs => Jobs.Count > 0
bool HasNoJobs => Jobs.Count == 0
bool HasSelectedJobs => SelectedJob != null
bool CanStartProcessing => HasJobs && IsNotProcessing
string OutputDirectory
int TotalJobsCount => Jobs.Count
int CompletedJobsCount => Jobs.Count(j => j.Status == Completed)
int ActiveJobsCount => Jobs.Count(j => j.IsProcessing)
int FailedJobsCount => Jobs.Count(j => j.Status == Failed)
double OverallProgressPercentage
string TotalElapsedTime
string EstimatedTimeRemaining
string ProcessingSpeed
string LogText
```

### JobViewModel Properties:
```
Guid Id
string FileName
string FullPath
string FileIcon (path to icon)
string FileSize (formatted: "245 MB")
string Duration (formatted: "23:45")
JobStatus Status (enum)
string StatusText ("Queued", "Converting", etc.)
Brush StatusColor (SolidColorBrush)
ProcessingPhase CurrentPhase (enum)
string PhaseText ("Phase: FFmpeg Conversion")
int Progress (0-100)
string ProgressText ("75%", "45% - Converting")
Brush ProgressBarColor
bool IsIndeterminate
string ElapsedTimeText ("05:34")
string EstimatedTimeRemaining ("~02:15 left")
bool IsSelected
bool IsProcessing
bool IsCompleted
bool IsFailed
bool CanRemove
DateTime? StartTime
DateTime? EndTime
string ErrorMessage
```

### Commands:
```
ICommand AddFilesCommand
ICommand RemoveSelectedCommand
ICommand StartProcessingCommand
ICommand PauseProcessingCommand
ICommand StopProcessingCommand
ICommand BrowseOutputFolderCommand
ICommand OpenSettingsCommand
ICommand OpenAboutCommand
ICommand ClearLogCommand
ICommand SaveLogCommand
ICommand ViewDetailsCommand (per job)
ICommand OpenOutputFolderCommand (per job)
ICommand OpenSubtitleFileCommand (per job)
ICommand RetryJobCommand (per job)
ICommand CancelJobCommand (per job)
ICommand RemoveJobCommand (per job)
ICommand OpenFileLocationCommand
ICommand CopyFilePathCommand
ICommand RetrySelectedCommand
ICommand ClearAllCommand
```

---

## 🎨 Styling & Theming Notes

### Color Palette:
```
Primary: #2196F3 (Blue)
Success: #4CAF50 (Green)
Warning: #FF9800 (Orange)
Error: #F44336 (Red)
Info: #9C27B0 (Purple)
Neutral: #757575 (Gray)
Background: #FAFAFA (Light Gray)
Border: #CCCCCC (Medium Gray)
Text: #212121 (Dark Gray)
TextSecondary: #757575 (Gray)
```

### Font Sizes:
```
Large Header: 16pt
Section Header: 14pt
Body: 12pt
Small: 10pt
Tiny: 9pt
Log: 10pt (Consolas)
```

### Spacing:
```
Section Margin: 5px
Control Padding: 10px
Button Padding: 10,5
Item Spacing: 5px
Row Height: 60px (DataGrid)
Border Thickness: 1px
```

### Icons:
```
Source: Material Design Icons hoặc Fluent UI System Icons
Size: 16x16 (toolbar), 24x24 (headers), 12x12 (status)
Format: SVG hoặc PNG with transparency
```

---

## 🔧 Additional Features

### Drag & Drop Support:
```
Window.AllowDrop = True
Window.Drop event handler:
  - Accept video files (*.mp4, *.mpeg, *.avi, *.mkv, *.mov)
  - Add to Jobs collection
  - Show visual feedback during drag
```

### Keyboard Shortcuts:
```
Ctrl+O: Add Files
Ctrl+S: Start Processing
Ctrl+P: Pause
Escape: Stop/Cancel
Delete: Remove Selected
F5: Refresh
Ctrl+,: Settings
Ctrl+L: Clear Log
```

### Window Properties:
```
Title: "Video Subtitle Generator"
Width: 1200
Height: 800
MinWidth: 900
MinHeight: 600
WindowStartupLocation: CenterScreen
Icon: App icon
ResizeMode: CanResize
```

### Status Bar (Optional):
```
Add Row 4 (Bottom status bar):
  StatusBar
    ├─ StatusBarItem: App version
    ├─ StatusBarItem: Python status (✓ Ready)
    ├─ StatusBarItem: FFmpeg status (✓ Ready)
    └─ StatusBarItem: Memory usage
```

---

## ✅ Implementation Checklist

### Phase 1: Basic Layout
- [ ] Create MainWindow.xaml
- [ ] Define Grid with 4 rows
- [ ] Add Toolbar StackPanel/Toolbar
- [ ] Add DataGrid container (GroupBox/Border)
- [ ] Add Progress Summary container
- [ ] Add Log Viewer container

### Phase 2: Toolbar Controls
- [ ] Add Files button
- [ ] Remove Selected button
- [ ] Start Processing button (styled)
- [ ] Pause button
- [ ] Stop button
- [ ] Output Folder button
- [ ] Output path TextBox
- [ ] Settings button
- [ ] About button
- [ ] Separators

### Phase 3: DataGrid Setup
- [ ] Configure DataGrid properties
- [ ] Add Checkbox column
- [ ] Add Filename column (with icon)
- [ ] Add Status column (with color indicator)
- [ ] Add Progress column (with ProgressBar + text)
- [ ] Add Time column
- [ ] Add Actions menu column
- [ ] Configure row style
- [ ] Add context menu
- [ ] Add empty state overlay

### Phase 4: Progress Summary
- [ ] Summary text (with runs)
- [ ] Overall ProgressBar
- [ ] Progress percentage overlay
- [ ] Statistics grid (3 columns)
- [ ] Total time display
- [ ] Estimated remaining display
- [ ] Processing speed display

### Phase 5: Log Viewer
- [ ] Log TextBox (styled)
- [ ] Clear button
- [ ] Save button
- [ ] Auto-scroll behavior
- [ ] Color coding (if using RichTextBox)

### Phase 6: Data Bindings
- [ ] Bind all toolbar buttons to commands
- [ ] Bind DataGrid ItemsSource
- [ ] Bind all column values
- [ ] Bind progress properties
- [ ] Bind log text
- [ ] Test all bindings

### Phase 7: Styling
- [ ] Apply color palette
- [ ] Button styles (normal, hover, pressed)
- [ ] ProgressBar style
- [ ] DataGrid style (alternating rows)
- [ ] Scrollbar style
- [ ] Border/GroupBox style

### Phase 8: Interactions
- [ ] Command implementations
- [ ] Drag & drop handler
- [ ] Keyboard shortcuts
- [ ] Context menus
- [ ] Tooltips
- [ ] Validation

### Phase 9: Visual States
- [ ] Idle state styling
- [ ] Processing state styling
- [ ] Paused state styling
- [ ] Completed state styling
- [ ] Error state styling

### Phase 10: Animations
- [ ] ProgressBar smooth animation
- [ ] Status change transitions
- [ ] Button hover effects
- [ ] Log entry fade-in
- [ ] Pulse animation for active status

---

## 📦 Resources Needed

### Icons:
- folder_plus.svg (Add Files)
- trash.svg (Remove)
- play.svg (Start)
- pause.svg (Pause)
- stop.svg (Stop)
- folder.svg (Output Folder)
- settings.svg (Settings)
- info.svg (About)
- video.svg (Video file icon)
- refresh.svg (Retry)
- checkmark.svg (Success)
- error.svg (Failed)

### Fonts:
- Segoe UI (default Windows font)
- Consolas (for log viewer)

### Colors: (Already defined in palette above)

---

## 🎯 Priority Order for AI UI Generation

### Must Have (Priority 1):
1. Basic Grid layout (4 rows)
2. Toolbar with main buttons
3. DataGrid with columns (esp. Progress column)
4. Data bindings setup
5. Basic styling

### Should Have (Priority 2):
6. Progress Summary section
7. Log Viewer
8. Context menus
9. Icons
10. Visual states

### Nice to Have (Priority 3):
11. Animations
12. Drag & drop
13. Keyboard shortcuts
14. Advanced styling
15. Status bar

---

## 💡 Notes for AI Generation

1. **Use WPF best practices**: MVVM pattern, data binding, commands
2. **Responsive design**: Use Star (*) sizing for flexible layouts
3. **Accessibility**: Include tooltips, keyboard shortcuts, high contrast support
4. **Performance**: Virtualize DataGrid for large lists
5. **Modern UI**: Flat design, subtle shadows, smooth animations
6. **Error handling**: Show user-friendly messages
7. **Feedback**: Visual feedback for all user actions
8. **Consistency**: Use consistent spacing, colors, fonts throughout

---

## 🔗 Related Documents

- `architecture-overview.md` - For ViewModel structure
- `final-architecture-decision.md` - For data flow
- `ui-design.md` - For overall UI structure

---

**Document Version**: 1.0
**Last Updated**: 2025-11-17
**Status**: Ready for UI Generation
