# Python Worker - Sample Log Output

## Example 1: Successful Processing

```
======================================================================
🎬 VIDEO SUBTITLE GENERATOR - PYTHON WORKER
======================================================================
🐍 Python version: 3.10.11
📂 Working directory: C:\Project\SubtitleGenerate\python-worker
📅 Started at: 2025-11-17 14:30:15
======================================================================
🔓 Decoding configuration...
✅ Configuration decoded successfully
📋 Configuration keys: ['input_file', 'output_dir', 'ffmpeg_path', 'whisper_model', 'language', 'device', 'fp16', 'task', 'output_format']

======================================================================
🎬 VIDEO SUBTITLE GENERATOR - PROCESSING START
======================================================================
🔧 Applying default configuration values...
✅ Validating configuration...
✅ Configuration validated successfully

📋 PROCESSING CONFIGURATION
----------------------------------------------------------------------
📁 Input file: C:\Videos\meeting.mp4
📂 Output directory: C:\Output
🤖 Whisper model: base
🌍 Language: English
💻 Device: cpu
📄 Output format: srt
🔧 Task: transcribe
----------------------------------------------------------------------

📂 Creating output directory: C:\Output
✅ Output directory ready: C:\Output

📝 OUTPUT FILES
   🔊 WAV: C:\Output\meeting.wav
   📄 Subtitle: C:\Output\meeting.srt

📊 Progress: [queued] 0% - Processing: meeting.mp4

============================================================
🎬 STEP 1: AUDIO EXTRACTION
============================================================
📁 Input video: C:\Videos\meeting.mp4
🔊 Output audio: C:\Output\meeting.wav
🛠️  FFmpeg path: ffmpeg
📊 Progress: [converting] 10% - Starting audio extraction...
📊 Input file size: 52,428,800 bytes (50.00 MB)
⏳ Running FFmpeg to extract audio (16kHz mono WAV)...
📊 Progress: [converting] 25% - Extracting audio with FFmpeg...
🚀 Starting FFmpeg subprocess...
✅ FFmpeg completed successfully (exit code 0)
✅ WAV file created successfully
📊 WAV file size: 5,242,880 bytes (5.00 MB)
📉 Size reduction: 90.0%
📊 Progress: [converting] 50% - Audio extraction completed
============================================================

============================================================
🤖 STEP 2: TRANSCRIPTION WITH WHISPER AI
============================================================
📁 Input WAV: C:\Output\meeting.wav
📄 Output subtitle: C:\Output\meeting.srt
✅ Whisper and Torch imported successfully
📊 WAV file size: 5,242,880 bytes (5.00 MB)
⏱️  Estimated audio duration: 327.68 seconds (5.46 minutes)
📊 Progress: [transcribing] 55% - Loading Whisper model...
🔧 Configuration:
   - Model: base
   - Device: cpu
⏳ Loading Whisper model (this may take a moment)...
💻 Using CPU for transcription
✅ Model loaded successfully in 3.45 seconds
📦 Model: base
📊 Progress: [transcribing] 65% - Model loaded: base
🔧 Transcription settings:
   - Language: English (code=auto-detect)
   - Task: transcribe
   - FP16: False
📊 Progress: [transcribing] 70% - Starting transcription...
🚀 Starting Whisper transcription...
✅ Transcription completed in 30.25 seconds
🌍 Detected language: en
📊 Generated 45 subtitle segments
⚡ Processing speed: 0.09x realtime
📊 Progress: [transcribing] 90% - Transcription completed, saving subtitle...
💾 Saving subtitle in format: srt
✅ Subtitle file created successfully
📊 Subtitle file size: 2,048 bytes (2.00 KB)
📈 Average subtitle density: 8.2 lines/minute
📊 Progress: [finalizing] 95% - Subtitle file created
============================================================

======================================================================
✅ PROCESSING COMPLETED SUCCESSFULLY
======================================================================
📊 Progress: [completed] 100% - Processing completed successfully

📊 FINAL STATISTICS
----------------------------------------------------------------------
⏱️  Total processing time: 46.12 seconds (0.77 minutes)
📁 Input video size: 52,428,800 bytes (50.00 MB)
🔊 WAV audio size: 5,242,880 bytes (5.00 MB)
📄 Subtitle file size: 2,048 bytes (2.00 KB)
📝 Base name: meeting
----------------------------------------------------------------------
✅ SUCCESS: Generated subtitle file: C:\Output\meeting.srt
📈 Metadata: {'input_size': '52428800', 'wav_size': '5242880', 'subtitle_size': '2048', 'duration_seconds': '46.12', 'base_name': 'meeting'}
🎉 All done! Subtitle file ready for use.
======================================================================

⏱️  Total execution time: 46.12 seconds (0.77 minutes)
📅 Finished at: 2025-11-17 14:31:01

======================================================================
✅ Process completed successfully (exit code: 0)
======================================================================
```

---

## Example 2: File Not Found Error

```
======================================================================
🎬 VIDEO SUBTITLE GENERATOR - PYTHON WORKER
======================================================================
🐍 Python version: 3.10.11
📂 Working directory: C:\Project\SubtitleGenerate\python-worker
📅 Started at: 2025-11-17 14:35:00
======================================================================
🔓 Decoding configuration...
✅ Configuration decoded successfully

======================================================================
🎬 VIDEO SUBTITLE GENERATOR - PROCESSING START
======================================================================
✅ Validating configuration...
❌ Configuration validation failed: Input file not found: C:\Videos\missing.mp4
❌ FAILED: Input file not found: C:\Videos\missing.mp4

⏱️  Total execution time: 0.05 seconds (0.00 minutes)
📅 Finished at: 2025-11-17 14:35:00

======================================================================
❌ Process failed (exit code: 1)
======================================================================
```

---

## Example 3: FFmpeg Error

```
============================================================
🎬 STEP 1: AUDIO EXTRACTION
============================================================
📁 Input video: C:\Videos\corrupted.mp4
🔊 Output audio: C:\Output\corrupted.wav
🛠️  FFmpeg path: ffmpeg
📊 Progress: [converting] 10% - Starting audio extraction...
📊 Input file size: 1,024,000 bytes (0.98 MB)
⏳ Running FFmpeg to extract audio (16kHz mono WAV)...
📊 Progress: [converting] 25% - Extracting audio with FFmpeg...
🚀 Starting FFmpeg subprocess...
❌ FFmpeg failed with exit code 1
📋 FFmpeg stderr: [mov,mp4,m4a,3gp,3g2,mj2 @ 0x000001] moov atom not found
Invalid data found when processing input

⏱️  Total execution time: 2.15 seconds (0.04 minutes)
📅 Finished at: 2025-11-17 14:40:23
```

---

## Example 4: CUDA Fallback

```
============================================================
🤖 STEP 2: TRANSCRIPTION WITH WHISPER AI
============================================================
📁 Input WAV: C:\Output\video.wav
📄 Output subtitle: C:\Output\video.srt
✅ Whisper and Torch imported successfully
📊 WAV file size: 8,388,608 bytes (8.00 MB)
⏱️  Estimated audio duration: 524.29 seconds (8.74 minutes)
📊 Progress: [transcribing] 55% - Loading Whisper model...
🔧 Configuration:
   - Model: medium
   - Device: cuda
⏳ Loading Whisper model (this may take a moment)...
⚠️  CUDA requested but not available, falling back to CPU
💻 Using CPU for transcription
✅ Model loaded successfully in 8.23 seconds
📦 Model: medium
```

---

## Example 5: Out of Memory

```
============================================================
🤖 STEP 2: TRANSCRIPTION WITH WHISPER AI
============================================================
📁 Input WAV: C:\Output\long_video.wav
📄 Output subtitle: C:\Output\long_video.srt
✅ Whisper and Torch imported successfully
📊 WAV file size: 104,857,600 bytes (100.00 MB)
⏱️  Estimated audio duration: 6553.60 seconds (109.23 minutes)
📊 Progress: [transcribing] 55% - Loading Whisper model...
🔧 Configuration:
   - Model: large
   - Device: cpu
⏳ Loading Whisper model (this may take a moment)...
✅ Model loaded successfully in 15.67 seconds
📊 Progress: [transcribing] 65% - Model loaded: large
🚀 Starting Whisper transcription...
💾 Out of memory during transcription. Try using a smaller model (tiny/base).
💡 Suggestion: Current model 'large' may be too large for available memory
❌ Transcription failed
❌ FAILED: Transcription failed
```

---

## Example 6: Verbose Mode with Debug Info

```bash
python process_media.py --config <base64> --verbose
```

```
======================================================================
🎬 VIDEO SUBTITLE GENERATOR - PYTHON WORKER
======================================================================
🐍 Python version: 3.10.11
📂 Working directory: C:\Project\SubtitleGenerate\python-worker
📅 Started at: 2025-11-17 15:00:00
======================================================================
🔍 Verbose logging enabled
🔓 Decoding configuration...
✅ Configuration decoded successfully
📋 Configuration keys: ['input_file', 'output_dir', 'ffmpeg_path', 'whisper_model', 'language', 'device', 'fp16', 'task', 'output_format']

======================================================================
🎬 VIDEO SUBTITLE GENERATOR - PROCESSING START
======================================================================
🔧 Applying default configuration values...
✅ Validating configuration...
✅ Configuration validated successfully

📋 PROCESSING CONFIGURATION
----------------------------------------------------------------------
📁 Input file: C:\Videos\test.mp4
📂 Output directory: C:\Output
🤖 Whisper model: base
🌍 Language: English
💻 Device: cpu
📄 Output format: srt
🔧 Task: transcribe
----------------------------------------------------------------------
🔍 Full configuration: {
  "input_file": "C:\\Videos\\test.mp4",
  "output_dir": "C:\\Output",
  "ffmpeg_path": "ffmpeg",
  "whisper_model": "base",
  "language": "English",
  "device": "cpu",
  "fp16": false,
  "task": "transcribe",
  "output_format": "srt"
}

...

🔧 FFmpeg command: ffmpeg -i C:\Videos\test.mp4 -vn -acodec pcm_s16le -ar 16000 -ac 1 -y C:\Output\test.wav
🚀 Starting FFmpeg subprocess...

...

🔍 Whisper version: 20231117
🔍 PyTorch version: 2.0.1

...

📝 First segment: [0.00s - 3.50s] Hello everyone, welcome to the meeting...
📝 Last segment: [324.00s - 327.68s] Thank you for your attention.

...

💾 Saving subtitle as srt: C:\Output\test.srt
✅ Subtitle saved successfully: C:\Output\test.srt
```

---

## Example 7: User Interruption (Ctrl+C)

```
🚀 Starting Whisper transcription...

======================================================================
⚠️  Process interrupted by user (Ctrl+C)
======================================================================
Process interrupted
(exit code: 2)
```

---

## Log File Location

All logs are saved to: `logs/worker_YYYYMMDD_HHMMSS.log`

Example: `logs/worker_20251117_143015.log`

## Emoji Legend

| Emoji | Meaning |
|-------|---------|
| 🎬 | Video/Processing start |
| 📁 | File path |
| 📂 | Directory |
| 🔊 | Audio file |
| 📄 | Subtitle file |
| 🤖 | AI/Whisper operations |
| 🛠️ | Tool/FFmpeg |
| 📊 | Statistics/Progress |
| ⏱️ | Time/Duration |
| ✅ | Success |
| ❌ | Error |
| ⚠️ | Warning |
| 💡 | Suggestion |
| 🔧 | Configuration |
| 🚀 | Process start |
| 💻 | CPU operations |
| 🎮 | GPU/CUDA operations |
| 💾 | Memory/Storage |
| 🌍 | Language detection |
| ⚡ | Performance metric |
| 🎉 | Completion |
| 🔍 | Debug info |
| 📋 | Details/Stack trace |

---

## Reading Logs

### View recent log
```bash
type logs\worker_20251117_143015.log
```

### Search for errors
```bash
findstr /i "❌" logs\*.log
```

### Search for specific file
```bash
findstr /i "meeting.mp4" logs\*.log
```

### View last 50 lines
```bash
powershell "Get-Content logs\worker_20251117_143015.log -Tail 50"
```
