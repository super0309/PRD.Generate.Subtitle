# Python Worker Design - process_media.py

## 🎯 Mục đích
Script Python nhận tham số từ C#, xử lý video → WAV → Subtitle, trả kết quả dạng JSON.

## 📝 Command Line Interface

### Arguments
```bash
python process_media.py \
  --input <video_file_path> \
  --output-dir <output_directory> \
  --model <whisper_model> \
  --language <language> \
  --device <cpu|cuda> \
  [--fp16 <True|False>] \
  [--task <transcribe|translate>] \
  [--output-format <srt|vtt|txt|json>] \
  [--keep-wav] \
  [--verbose]
```

### Required Arguments:
- `--input`: Đường dẫn file video input
- `--output-dir`: Thư mục lưu output files
- `--model`: Whisper model (tiny, base, small, medium, large)
- `--language`: Ngôn ngữ (English, Vietnamese, etc.)
- `--device`: cpu hoặc cuda

### Optional Arguments:
- `--fp16`: Enable FP16 (default: False cho CPU)
- `--task`: transcribe hoặc translate (default: transcribe)
- `--output-format`: Format phụ đề (default: srt)
- `--keep-wav`: Giữ lại file WAV sau khi xử lý (default: False)
- `--verbose`: In chi tiết log (default: False)

## 🏗️ Script Structure

```python
# process_media.py

import sys
import json
import argparse
import subprocess
import os
from pathlib import Path
import time

def main():
    # 1. Parse arguments
    args = parse_arguments()
    
    # 2. Validate inputs
    if not validate_inputs(args):
        output_error("Invalid inputs")
        sys.exit(1)
    
    # 3. Convert to WAV
    wav_file = convert_to_wav(args.input, args.output_dir)
    if not wav_file:
        output_error("FFmpeg conversion failed")
        sys.exit(1)
    
    # 4. Transcribe with Whisper
    subtitle_file = transcribe_audio(wav_file, args)
    if not subtitle_file:
        output_error("Whisper transcription failed")
        sys.exit(1)
    
    # 5. Cleanup (delete WAV if needed)
    if not args.keep_wav:
        try:
            os.remove(wav_file)
        except:
            pass
    
    # 6. Output success result
    output_success(wav_file, subtitle_file, args)
    sys.exit(0)

def parse_arguments():
    parser = argparse.ArgumentParser()
    parser.add_argument('--input', required=True)
    parser.add_argument('--output-dir', required=True)
    parser.add_argument('--model', required=True)
    parser.add_argument('--language', required=True)
    parser.add_argument('--device', required=True)
    parser.add_argument('--fp16', default='False')
    parser.add_argument('--task', default='transcribe')
    parser.add_argument('--output-format', default='srt')
    parser.add_argument('--keep-wav', action='store_true')
    parser.add_argument('--verbose', action='store_true')
    return parser.parse_args()

def validate_inputs(args):
    # Check if input file exists
    if not os.path.isfile(args.input):
        return False
    
    # Check if output dir exists, create if not
    os.makedirs(args.output_dir, exist_ok=True)
    
    return True

def convert_to_wav(input_file, output_dir):
    """
    Convert video to WAV 16kHz mono using FFmpeg
    """
    try:
        # Generate output WAV path
        input_path = Path(input_file)
        wav_filename = input_path.stem + '.wav'
        wav_path = os.path.join(output_dir, wav_filename)
        
        # Build FFmpeg command
        ffmpeg_cmd = [
            'ffmpeg',
            '-y',  # Overwrite output file
            '-i', input_file,
            '-ar', '16000',  # Sample rate 16kHz
            '-ac', '1',      # Mono
            '-c:a', 'pcm_s16le',  # PCM 16-bit
            wav_path
        ]
        
        # Run FFmpeg
        result = subprocess.run(
            ffmpeg_cmd,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            text=True
        )
        
        if result.returncode != 0:
            print(f"FFmpeg error: {result.stderr}", file=sys.stderr)
            return None
        
        return wav_path
        
    except Exception as e:
        print(f"Exception in convert_to_wav: {str(e)}", file=sys.stderr)
        return None

def transcribe_audio(wav_file, args):
    """
    Transcribe audio using Whisper
    """
    try:
        # Build Whisper command
        whisper_cmd = [
            'whisper',
            wav_file,
            '--model', args.model,
            '--language', args.language,
            '--task', args.task,
            '--device', args.device,
            '--fp16', args.fp16,
            '--output_format', args.output_format,
            '--output_dir', args.output_dir
        ]
        
        # Run Whisper
        result = subprocess.run(
            whisper_cmd,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            text=True
        )
        
        if result.returncode != 0:
            print(f"Whisper error: {result.stderr}", file=sys.stderr)
            return None
        
        # Find generated subtitle file
        wav_path = Path(wav_file)
        subtitle_file = os.path.join(
            args.output_dir,
            wav_path.stem + '.' + args.output_format
        )
        
        if os.path.isfile(subtitle_file):
            return subtitle_file
        else:
            return None
            
    except Exception as e:
        print(f"Exception in transcribe_audio: {str(e)}", file=sys.stderr)
        return None

def output_success(wav_file, subtitle_file, args):
    """
    Output success JSON to stdout
    """
    result = {
        'status': 'success',
        'wav_file': wav_file if args.keep_wav else None,
        'subtitle_file': subtitle_file,
        'error': None,
        'metadata': {
            'model': args.model,
            'language': args.language,
            'device': args.device,
            'output_format': args.output_format
        }
    }
    
    print(json.dumps(result, ensure_ascii=False))

def output_error(error_message):
    """
    Output error JSON to stdout
    """
    result = {
        'status': 'error',
        'wav_file': None,
        'subtitle_file': None,
        'error': error_message,
        'metadata': None
    }
    
    print(json.dumps(result, ensure_ascii=False))

if __name__ == '__main__':
    main()
```

## 📤 Output Format

### Success Response
```json
{
  "status": "success",
  "wav_file": "C:\\output\\video.wav",
  "subtitle_file": "C:\\output\\video.srt",
  "error": null,
  "metadata": {
    "model": "small",
    "language": "English",
    "device": "cpu",
    "output_format": "srt"
  }
}
```

### Error Response
```json
{
  "status": "error",
  "wav_file": null,
  "subtitle_file": null,
  "error": "FFmpeg not found in PATH",
  "metadata": null
}
```

## 🔍 Error Handling

### Các loại lỗi cần handle:

1. **Input File Errors**
   - File không tồn tại
   - File bị corrupt
   - Không có quyền đọc

2. **FFmpeg Errors**
   - FFmpeg không được cài đặt
   - Codec không support
   - Hết dung lượng disk

3. **Whisper Errors**
   - Model không tồn tại
   - Hết RAM/VRAM
   - Language không hợp lệ
   - Audio file bị lỗi

4. **Output Errors**
   - Không có quyền ghi vào output_dir
   - Disk full

### Error Handling Strategy:
```python
try:
    # Main processing
except FileNotFoundError as e:
    output_error(f"File not found: {str(e)}")
except PermissionError as e:
    output_error(f"Permission denied: {str(e)}")
except subprocess.CalledProcessError as e:
    output_error(f"Process failed: {str(e)}")
except Exception as e:
    output_error(f"Unexpected error: {str(e)}")
finally:
    # Cleanup temp files
```

## 🧪 Testing

### Unit Tests
```python
# test_process_media.py

def test_valid_input():
    # Test với video hợp lệ
    pass

def test_invalid_input():
    # Test với file không tồn tại
    pass

def test_ffmpeg_conversion():
    # Test FFmpeg convert
    pass

def test_whisper_transcription():
    # Test Whisper transcribe
    pass

def test_json_output():
    # Verify JSON format
    pass
```

### Manual Testing
```bash
# Test case 1: Normal case
python process_media.py \
  --input "test_video.mp4" \
  --output-dir "./output" \
  --model small \
  --language English \
  --device cpu

# Test case 2: Keep WAV
python process_media.py \
  --input "test_video.mp4" \
  --output-dir "./output" \
  --model small \
  --language English \
  --device cpu \
  --keep-wav

# Test case 3: Invalid input
python process_media.py \
  --input "nonexistent.mp4" \
  --output-dir "./output" \
  --model small \
  --language English \
  --device cpu
```

## 📦 Dependencies (requirements.txt)

```txt
openai-whisper>=20230314
torch>=2.0.0
torchaudio>=2.0.0
ffmpeg-python>=0.2.0
```

### Installation
```bash
# Create virtual environment
python -m venv venv

# Activate (Windows)
.\venv\Scripts\activate

# Install dependencies
pip install -r requirements.txt

# Install FFmpeg separately (if not in PATH)
# Download from: https://ffmpeg.org/download.html
```

## 🔧 Configuration

### Python Environment Setup Script
```bash
# setup_python_env.ps1 (PowerShell)

Write-Host "Setting up Python environment..."

# Check Python version
$pythonVersion = python --version
Write-Host "Python version: $pythonVersion"

# Create venv
if (!(Test-Path ".\python-worker\venv")) {
    python -m venv .\python-worker\venv
}

# Activate venv
.\python-worker\venv\Scripts\Activate.ps1

# Upgrade pip
python -m pip install --upgrade pip

# Install requirements
pip install -r .\python-worker\requirements.txt

# Test imports
python -c "import whisper; print('Whisper OK')"

Write-Host "Setup complete!"
```

## 🚀 Performance Optimization

### 1. Model Loading
- Load model một lần, reuse cho nhiều files (nếu batch processing)
- Cache model in memory

### 2. Multiprocessing
- Có thể xử lý nhiều file song song
- Mỗi process xử lý 1 file

### 3. GPU Acceleration
- Detect CUDA availability
- Use GPU nếu có để tăng tốc

```python
import torch

def get_device():
    if torch.cuda.is_available():
        return 'cuda'
    else:
        return 'cpu'
```

## 📊 Progress Reporting (Advanced)

### Option 1: Stdout Progress Lines
```python
print("PROGRESS:25:Converting to WAV")
print("PROGRESS:50:Loading Whisper model")
print("PROGRESS:75:Transcribing...")
print("PROGRESS:100:Complete")
```

C# parse những dòng này để update progress bar.

### Option 2: Separate Progress File
```python
progress_file = os.path.join(args.output_dir, f"{job_id}_progress.json")

def update_progress(percent, message):
    with open(progress_file, 'w') as f:
        json.dump({'percent': percent, 'message': message}, f)
```

C# đọc file này định kỳ.

## 🐛 Debugging

### Verbose Mode
```python
if args.verbose:
    print(f"[DEBUG] Input file: {args.input}", file=sys.stderr)
    print(f"[DEBUG] Output dir: {args.output_dir}", file=sys.stderr)
    print(f"[DEBUG] Running FFmpeg...", file=sys.stderr)
```

### Log File
```python
import logging

logging.basicConfig(
    filename=os.path.join(args.output_dir, 'process.log'),
    level=logging.DEBUG
)

logging.info("Starting processing...")
logging.debug(f"Arguments: {args}")
```

## ✅ Validation Checklist

- [ ] Arguments validation
- [ ] Input file existence check
- [ ] Output directory creation
- [ ] FFmpeg availability check
- [ ] Whisper model download check
- [ ] Disk space check
- [ ] Error handling cho mọi operation
- [ ] JSON output format correct
- [ ] Exit codes correct (0/1)
- [ ] Cleanup temp files
- [ ] Works on Windows + Linux
