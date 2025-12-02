# Python Worker - Enhanced Error Handling & Logging

## 📋 Improvements Summary

### ✅ Added Comprehensive Logging

**File Logging:**
- Log files stored in `logs/` directory
- Filename format: `worker_YYYYMMDD_HHMMSS.log`
- UTF-8 encoding for international characters
- Automatic log directory creation

**Console Logging:**
- Errors sent to stderr (for C# to capture)
- Progress sent to stdout (JSON format)
- Dual output: file + console

**Log Levels:**
- `DEBUG` - Detailed diagnostic information
- `INFO` - General information (default)
- `WARNING` - Warning messages
- `ERROR` - Error messages with context
- `CRITICAL` - Fatal errors

### ✅ Enhanced Exception Handling

**Global Try-Catch Structure:**
```python
try:
    # Main processing
except KeyError as e:
    # Missing config parameters
except FileNotFoundError as e:
    # File not found
except PermissionError as e:
    # Permission denied
except MemoryError as e:
    # Out of memory
except subprocess.TimeoutExpired:
    # FFmpeg timeout
except KeyboardInterrupt:
    # User cancellation
except Exception as e:
    # Catch-all
finally:
    # Cleanup and duration logging
```

**Function-Level Error Handling:**
- Each function has its own try-catch
- Detailed error messages with context
- Stack trace logging for debugging
- Graceful error propagation

### ✅ Input Validation

**File Validation:**
- Check input file exists before processing
- Verify output files created after operations
- Log file sizes (bytes, MB, KB)

**Configuration Validation:**
- Required fields check (input_file, output_dir)
- CUDA availability check (fallback to CPU)
- Model existence verification

**Process Validation:**
- FFmpeg exit code checking
- Whisper import verification
- Output format validation

### ✅ Detailed Progress Logging

**Processing Steps Logged:**
1. Configuration received and decoded
2. Input file validation
3. Output directory creation
4. FFmpeg audio extraction (with file sizes)
5. Whisper model loading
6. Transcription (with duration)
7. Detected language
8. Segment count
9. Subtitle file saving
10. Final metadata

**Example Log Output:**
```
2025-11-17 14:30:15 [INFO] ============================================================
2025-11-17 14:30:15 [INFO] Starting media processing
2025-11-17 14:30:15 [INFO] Input file: C:\Videos\sample.mp4
2025-11-17 14:30:15 [INFO] Input file size: 52,428,800 bytes (50.00 MB)
2025-11-17 14:30:20 [INFO] WAV file created: 5,242,880 bytes (5.00 MB)
2025-11-17 14:30:25 [INFO] Loading Whisper model: base on device: cpu
2025-11-17 14:30:30 [INFO] Model loaded successfully: base
2025-11-17 14:31:00 [INFO] Transcription completed in 30.25 seconds
2025-11-17 14:31:00 [INFO] Detected language: en
2025-11-17 14:31:00 [INFO] Generated 45 segments
2025-11-17 14:31:01 [INFO] Subtitle file created: 2,048 bytes (2.00 KB)
2025-11-17 14:31:01 [INFO] Processing completed successfully in 46.12 seconds
2025-11-17 14:31:01 [INFO] ============================================================
```

### ✅ Timeout Protection

**FFmpeg Timeout:**
- 1 hour maximum execution time
- Prevents hanging on corrupted files
- Graceful timeout handling

### ✅ Metadata Enhancements

**Extended Metadata:**
```json
{
  "input_size": "52428800",
  "wav_size": "5242880",
  "subtitle_size": "2048",
  "duration_seconds": "46.12",
  "base_name": "sample"
}
```

### ✅ New Configuration Module (`config.py`)

**Features:**
- Default configuration values
- Configuration validation
- Valid value constants
- Language code mapping

**Usage:**
```python
from config import validate_config, apply_defaults

# Validate
is_valid, error = validate_config(config)

# Apply defaults
config = apply_defaults(config)
```

### ✅ Command Line Arguments

**New Arguments:**
```bash
python process_media.py --config <base64> [--log-dir logs] [--verbose]
```

- `--log-dir`: Custom log directory
- `--verbose`: Enable DEBUG level logging

### ✅ Error Messages

**User-Friendly Errors:**
- Clear error descriptions
- Actionable suggestions
- Context information

**Examples:**
```
[ERROR] Input file not found: C:\Videos\missing.mp4
[ERROR] FFmpeg failed with exit code 1: Invalid input format
[ERROR] Out of memory during transcription. Try using a smaller model (tiny/base).
[ERROR] Whisper import failed. Please install: pip install openai-whisper torch
```

## 📊 Error Handling Coverage

| Error Type | Detection | Logging | Recovery |
|-----------|-----------|---------|----------|
| Missing config | ✅ | ✅ | ✅ Return error |
| File not found | ✅ | ✅ | ✅ Return error |
| Permission denied | ✅ | ✅ | ✅ Return error |
| FFmpeg failure | ✅ | ✅ | ✅ Return error |
| FFmpeg timeout | ✅ | ✅ | ✅ Return error |
| Whisper import fail | ✅ | ✅ | ✅ Return error |
| Out of memory | ✅ | ✅ | ✅ Suggest smaller model |
| CUDA unavailable | ✅ | ✅ | ✅ Fallback to CPU |
| Keyboard interrupt | ✅ | ✅ | ✅ Exit code 2 |
| Unknown errors | ✅ | ✅ | ✅ Stack trace logged |

## 📁 File Structure

```
python-worker/
├── process_media.py      # Main worker (enhanced with logging)
├── config.py             # Configuration module (new)
├── requirements.txt      # Dependencies
├── .gitignore           # Git ignore file (new)
├── setup.bat            # Setup script
├── test_worker.py       # Test script
├── README.md            # Documentation
└── logs/                # Log files (auto-created)
    └── worker_20251117_143015.log
```

## 🔍 Debugging Features

**Stack Traces:**
- Full stack traces logged at DEBUG level
- Exception type and message logged
- Function call hierarchy preserved

**Verbose Mode:**
```bash
python process_media.py --config <base64> --verbose
```

**Log File Analysis:**
```bash
# View logs
type logs\worker_*.log

# Search for errors
findstr /i "error" logs\worker_*.log

# Search for warnings
findstr /i "warning" logs\worker_*.log
```

## 🎯 Exit Codes

| Code | Meaning |
|------|---------|
| 0 | Success |
| 1 | Error (processing failed) |
| 2 | Interrupted (Ctrl+C) |

## 🔗 Integration with C#

**C# PythonWorkerService automatically:**
- Captures stdout (progress JSON)
- Captures stderr (error messages)
- Parses log messages
- Handles exit codes
- Reports to UI

**No changes needed** in C# code - it already handles all outputs correctly!

## ✅ Testing Checklist

- [ ] Normal processing (success case)
- [ ] Missing input file
- [ ] Invalid configuration
- [ ] FFmpeg not found
- [ ] Whisper not installed
- [ ] Out of memory (large file + large model)
- [ ] User cancellation (Ctrl+C)
- [ ] Permission denied (read-only directory)
- [ ] Invalid output format
- [ ] CUDA requested but unavailable
- [ ] Timeout (very long video)

## 📚 Related Files

- `c:\Project\SubtitleGenerate\src\VideoSubtitleGenerator.Infrastructure\Services\PythonWorkerService.cs` - C# integration
- `c:\Project\SubtitleGenerate\python-worker\README.md` - User documentation
- `c:\Project\SubtitleGenerate\IMPLEMENTATION-STATUS.md` - Overall project status

---

**Status:** ✅ Python Worker fully enhanced with production-grade error handling and logging!
