# Python Worker - Video Subtitle Generator

Python script for processing video files: audio extraction (FFmpeg) and subtitle generation (Whisper AI).

## Requirements

### System Requirements
- Python 3.8 or higher
- FFmpeg installed and in PATH
- 4GB+ RAM (8GB+ recommended for larger models)

### Python Dependencies

Install required packages:

```bash
pip install -r requirements.txt
```

**Dependencies:**
- `openai-whisper` - OpenAI Whisper for speech recognition
- `torch` - PyTorch for deep learning
- `torchaudio` - Audio processing
- `numpy` - Numerical operations

### GPU Support (Optional)

For faster processing with NVIDIA GPU:

```bash
# Install CUDA-enabled PyTorch
pip install torch torchvision torchaudio --index-url https://download.pytorch.org/whl/cu118
```

## Usage

### Command Line

```bash
python process_media.py --config <base64_encoded_json>
```

### Configuration Format

The script expects a Base64-encoded JSON configuration:

```json
{
  "input_file": "C:\\Videos\\sample.mp4",
  "output_dir": "C:\\Output",
  "ffmpeg_path": "ffmpeg",
  "whisper_model": "base",
  "language": "English",
  "device": "cpu",
  "fp16": false,
  "task": "transcribe",
  "output_format": "srt"
}
```

### Configuration Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `input_file` | string | *required* | Path to input video file |
| `output_dir` | string | *required* | Directory for output files |
| `ffmpeg_path` | string | `"ffmpeg"` | Path to FFmpeg executable |
| `whisper_model` | string | `"base"` | Whisper model size (tiny/base/small/medium/large) |
| `language` | string | `"English"` | Target language (English/Vietnamese/auto) |
| `device` | string | `"cpu"` | Processing device (cpu/cuda) |
| `fp16` | boolean | `false` | Use FP16 precision (GPU only) |
| `task` | string | `"transcribe"` | Task type (transcribe/translate) |
| `output_format` | string | `"srt"` | Subtitle format (srt/vtt/txt/json) |

## Output

### Progress Updates (stdout)

The script reports progress as JSON to stdout:

```json
{"phase": "converting", "percent": 25, "message": "Extracting audio with FFmpeg..."}
{"phase": "transcribing", "percent": 70, "message": "Starting transcription..."}
{"phase": "completed", "percent": 100, "message": "Processing completed successfully"}
```

**Phases:**
- `queued` - Job queued
- `converting` - Audio extraction (0-50%)
- `transcribing` - Whisper transcription (50-95%)
- `finalizing` - Saving subtitle file (95-100%)
- `completed` - Successfully completed
- `failed` - Processing failed

### Final Result (stdout)

```json
{
  "result": {
    "success": true,
    "wav_file": "C:\\Output\\sample.wav",
    "subtitle_file": "C:\\Output\\sample.srt",
    "error": null,
    "metadata": {
      "input_size": "52428800",
      "wav_size": "5242880",
      "subtitle_size": "2048"
    }
  }
}
```

### Error Messages (stderr)

Errors are written to stderr for logging:

```
FFmpeg error: Invalid input file format
Whisper import failed: No module named 'whisper'
Processing failed: Out of memory
```

## Whisper Models

| Model | Size | VRAM | Speed | Quality |
|-------|------|------|-------|---------|
| tiny | 39M | ~1GB | ~32x | Low |
| base | 74M | ~1GB | ~16x | Fair |
| small | 244M | ~2GB | ~6x | Good |
| medium | 769M | ~5GB | ~2x | Very Good |
| large | 1550M | ~10GB | ~1x | Best |

**Recommendation:** Start with `base` or `small` for testing.

## Supported Formats

### Input Video Formats
- MP4, AVI, MKV, MOV, WMV, FLV
- Any format supported by FFmpeg

### Output Subtitle Formats
- **SRT** (SubRip) - Default, widely supported
- **VTT** (WebVTT) - Web-based video players
- **TXT** - Plain text transcript
- **JSON** - Full Whisper output with timestamps

## Examples

### Example 1: Basic Usage (from C#)

```csharp
var config = new {
    input_file = "C:\\Videos\\meeting.mp4",
    output_dir = "C:\\Subtitles",
    ffmpeg_path = "ffmpeg",
    whisper_model = "base",
    language = "English",
    device = "cpu",
    fp16 = false,
    task = "transcribe",
    output_format = "srt"
};

var configJson = JsonSerializer.Serialize(config);
var configBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(configJson));

var args = $"process_media.py --config {configBase64}";
```

### Example 2: Vietnamese Language

```json
{
  "language": "Vietnamese",
  "whisper_model": "small"
}
```

### Example 3: GPU Acceleration

```json
{
  "device": "cuda",
  "fp16": true,
  "whisper_model": "medium"
}
```

### Example 4: Translation to English

```json
{
  "task": "translate",
  "language": "Vietnamese"
}
```

## Troubleshooting

### Issue: "FFmpeg not found"
**Solution:** Install FFmpeg and add to PATH
```bash
# Windows (using Chocolatey)
choco install ffmpeg

# Or download from: https://ffmpeg.org/download.html
```

### Issue: "No module named 'whisper'"
**Solution:** Install requirements
```bash
pip install -r requirements.txt
```

### Issue: Out of memory
**Solution:** Use a smaller model
```json
{"whisper_model": "tiny"}  // or "base"
```

### Issue: Slow processing on CPU
**Solution:** Use GPU if available
```json
{"device": "cuda", "fp16": true}
```

### Issue: Incorrect language detection
**Solution:** Specify language explicitly
```json
{"language": "Vietnamese"}  // instead of "auto"
```

## Performance Tips

1. **CPU Processing:**
   - Use `tiny` or `base` models
   - Process sequentially (one at a time)
   - Close other applications

2. **GPU Processing:**
   - Use `small` or `medium` models
   - Enable FP16 for 2x speedup
   - Monitor VRAM usage

3. **Batch Processing:**
   - Process shorter videos in parallel
   - Process longer videos sequentially
   - Use SSD for faster I/O

## Integration with C#

The C# application (`PythonWorkerService.cs`) handles:
- Configuration encoding (Base64)
- Process lifecycle management
- Progress parsing from stdout
- Error handling from stderr
- Cancellation token support

## License

This script is part of the Video Subtitle Generator project.
