# Sửa lỗi Progress Reporting - Summary

## 🐛 Vấn đề tìm thấy

### 1. **Syntax Error trong Python** (CRITICAL)
**File**: `python-worker/process_media.py` line 74-75

**Lỗi**:
```python
progress = 
{
    "phase": phase,
```
❌ Dấu `=` và `{` bị xuống dòng → **Python Syntax Error!**

**Đã sửa**:
```python
progress = {
    "phase": phase,
```

### 2. **Path không khớp giữa Python và C#** (HIGH)

**Python** (SAI):
```python
progress_file = os.path.join(os.path.dirname(sys.argv[0]), f"{job_id}_progress.json")
```
- `sys.argv[0]` = path đến Python interpreter HOẶC script path (không consistent)
- Có thể trả về: `C:\Python39\python.exe` → dirname = `C:\Python39` ❌

**C#** (SAI):
```csharp
var progressFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "python-worker", $"{job.Id}_progress.json");
```
- `AppDomain.CurrentDomain.BaseDirectory` = thư mục chứa `.exe` (ví dụ: `bin\Debug\net6.0-windows`)
- Kết quả: `bin\Debug\net6.0-windows\python-worker\{guid}_progress.json`
- Nhưng script nằm ở: `c:\Project\PRD.Generate.Subttitle\python-worker\`
- → **KHÔNG KHỚP!**

### 3. **Missing Logging**
- Không log path của progress file → khó debug

## ✅ Giải pháp đã implement

### Python side (`process_media.py`)

```python
def report_progress(phase: str, percent: int, message: str):
    """Report progress to stdout as JSON for C# to parse, AND write to progress file"""
    logging.info(f"📊 REPORT_PROGRESS CALLED: {phase} {percent}% - job_id={job_id}")
    
    progress = {  # ✅ Fixed syntax
        "phase": phase,
        "percent": percent,
        "message": message,
        "timestamp": datetime.now().isoformat()
    }
    
    print(json.dumps(progress), flush=True)
    
    try:
        if job_id is not None:
            # ✅ Use __file__ to get script directory (reliable)
            script_dir = os.path.dirname(os.path.abspath(__file__))
            progress_file = os.path.join(script_dir, f"{job_id}_progress.json")
            
            logging.debug(f"📝 Writing progress to: {progress_file}")  # ✅ Added logging
            with open(progress_file, 'w', encoding='utf-8') as f:
                json.dump(progress, f, ensure_ascii=False, indent=2)
    except Exception as e:
        logging.warning(f"⚠️  Could not write progress file: {e}")  # ✅ Better error message
```

**Thay đổi**:
1. ✅ Fixed syntax error (`progress = {` trên cùng dòng)
2. ✅ Dùng `os.path.abspath(__file__)` thay vì `sys.argv[0]`
3. ✅ Thêm logging để debug
4. ✅ Better error messages

### C# side (`PythonWorkerService.cs`)

```csharp
// Set up file-based progress polling as reliable fallback
// Progress file is written to the same directory as the Python script
var scriptDir = Path.GetDirectoryName(Path.GetFullPath(_settings.Python.ScriptPath)) ?? 
               Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "python-worker");
var progressFile = Path.Combine(scriptDir, $"{job.Id}_progress.json");

_logService.LogDebug($"📝 Polling progress file at: {progressFile}");

var progressTimer = new System.Timers.Timer(500);
progressTimer.Elapsed += (s, e) =>
{
    try
    {
        if (File.Exists(progressFile))
        {
            var json = File.ReadAllText(progressFile);
            var progressData = JsonSerializer.Deserialize<ProgressMessage>(json);
            if (progressData != null && progress != null)
            {
                _logService.LogDebug($"📊 Progress from file: {progressData.Phase} {progressData.Percent}%");
                progress.Report(new JobProgress
                {
                    Phase = MapPhase(progressData.Phase),
                    Percent = progressData.Percent,
                    Message = progressData.Message
                });
            }
        }
    }
    catch (Exception ex)
    {
        _logService.LogDebug($"⚠️  Progress file read error: {ex.Message}");
    }
};
```

**Thay đổi**:
1. ✅ Tính `scriptDir` từ `_settings.Python.ScriptPath` (reliable!)
2. ✅ Fallback về `AppDomain.CurrentDomain.BaseDirectory + python-worker`
3. ✅ Log progress file path để debug
4. ✅ Log mỗi lần đọc được progress
5. ✅ Log errors thay vì silent catch

## 🧪 Test

Tạo `test_progress.py` để verify:

```bash
cd python-worker
python test_progress.py
```

**Kết quả**:
```
✅ Successfully created progress file
   File exists: True
   Content verified: {'phase': 'testing', 'percent': 50, ...}
✅ Test completed successfully!
```

## 🎯 Kết quả

Sau khi sửa:

1. ✅ **Syntax error đã fix** → Python script không crash
2. ✅ **Path khớp nhau** → Python ghi và C# đọc cùng 1 file
3. ✅ **Logging đầy đủ** → Dễ debug
4. ✅ **job_id được truyền đúng** từ C# → Python qua config
5. ✅ **Progress file được tạo đúng thư mục**

## 📝 Những gì còn lại cần test

1. **Integration test**: Chạy full pipeline với 1 video thật
2. **Verify cleanup**: Đảm bảo progress files bị xóa sau khi xong
3. **Concurrent jobs**: Test nhiều jobs chạy cùng lúc (mỗi job có progress file riêng)

## 💡 Tips debug

Nếu vẫn không hoạt động:

1. Check log file: `python-worker/logs/worker_*.log`
2. Check C# log: `logs/app.log`
3. Verify `appsettings.json` có đúng path đến Python script không
4. Manually tạo progress file và xem C# có đọc được không
