#!/usr/bin/env python3
"""
Video Subtitle Generator - Python Worker
Processes video files: extracts audio (FFmpeg) and generates subtitles (Whisper AI)
"""

import sys
import os
import json
import base64
import argparse
import subprocess
import logging
import traceback
from pathlib import Path
from typing import Dict, Any, Optional
from datetime import datetime

# Try to import config module (optional)
try:
    import config
    from config import apply_defaults, validate_config
    CONFIG_MODULE_AVAILABLE = True
except ImportError:
    CONFIG_MODULE_AVAILABLE = False
    logging.debug("ℹ️  config.py module not available - using defaults")


def setup_logging(log_dir: str = "logs"):
    """Setup logging configuration"""
    try:
        # Create log directory if it doesn't exist
        os.makedirs(log_dir, exist_ok=True)
        
        # Generate log filename with timestamp
        log_filename = f"worker_{datetime.now().strftime('%Y%m%d_%H%M%S')}.log"
        log_filepath = os.path.join(log_dir, log_filename)
        
        # Configure logging
        logging.basicConfig(
            level=logging.INFO,
            format='[%(asctime)s] [%(levelname)s] %(message)s',
            datefmt='%Y-%m-%d %H:%M:%S',
            handlers=[
                logging.FileHandler(log_filepath, encoding='utf-8'),
                logging.StreamHandler(sys.stderr)  # Also log to stderr for C# to capture
            ]
        )
        
        logging.info(f"📁 Log file: {log_filepath}")
        
    except Exception as e:
        # Fallback to basic logging if file creation fails
        logging.basicConfig(
            level=logging.INFO,
            format='[%(asctime)s] [%(levelname)s] %(message)s',
            datefmt='%Y-%m-%d %H:%M:%S',
            handlers=[logging.StreamHandler(sys.stderr)]
        )
        logging.warning(f"⚠️  Could not create log file: {e}")


def report_progress(phase: str, percent: int, message: str):
    """Report progress to stdout as JSON for C# to parse"""
    progress = {
        "phase": phase,
        "percent": percent,
        "message": message
    }
    print(json.dumps(progress), flush=True)


def report_result(success: bool, wav_file: Optional[str] = None, 
                  subtitle_file: Optional[str] = None, 
                  error: Optional[str] = None,
                  metadata: Optional[Dict[str, str]] = None):
    """Report final result to stdout as JSON"""
    try:
        result = {
            "result": {
                "success": success,
                "wav_file": wav_file,
                "subtitle_file": subtitle_file,
                "error": error,
                "metadata": metadata or {}
            }
        }
        print(json.dumps(result), flush=True)
        
        if success:
            logging.info(f"✅ SUCCESS: Generated subtitle file: {subtitle_file}")
            if metadata:
                logging.info(f"📈 Metadata: {metadata}")
        else:
            logging.error(f"❌ FAILED: {error}")
    except Exception as e:
        logging.error(f"❌ Failed to report result: {e}")


def extract_audio(input_file: str, output_wav: str, ffmpeg_path: str = "ffmpeg") -> bool:
    """Extract audio from video using FFmpeg"""
    try:
        logging.info("=" * 60)
        logging.info("🎬 STEP 1: AUDIO EXTRACTION")
        logging.info("=" * 60)
        logging.info(f"📁 Input video: {input_file}")
        logging.info(f"🔊 Output audio: {output_wav}")
        logging.info(f"🛠️  FFmpeg path: {ffmpeg_path}")
        
        report_progress("converting", 10, "Starting audio extraction...")
        
        # Validate input file
        if not os.path.exists(input_file):
            error_msg = f"Input file not found: {input_file}"
            logging.error(f"❌ {error_msg}")
            raise FileNotFoundError(error_msg)
        
        file_size = os.path.getsize(input_file)
        logging.info(f"📊 Input file size: {file_size:,} bytes ({file_size / (1024*1024):.2f} MB)")
        
        # FFmpeg command to extract audio as WAV (16kHz, mono)
        cmd = [
            ffmpeg_path,
            "-i", input_file,
            "-vn",  # No video
            "-acodec", "pcm_s16le",  # PCM 16-bit
            "-ar", "16000",  # 16kHz sample rate
            "-ac", "1",  # Mono
            "-y",  # Overwrite output
            output_wav
        ]
        
        logging.debug(f"🔧 FFmpeg command: {' '.join(cmd)}")
        logging.info("⏳ Running FFmpeg to extract audio (16kHz mono WAV)...")
        report_progress("converting", 25, "Extracting audio with FFmpeg...")
        
        # Run FFmpeg
        logging.debug("🚀 Starting FFmpeg subprocess...")
        result = subprocess.run(
            cmd,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            text=True,
            encoding='utf-8',
            errors='replace',
            timeout=3600  # 1 hour timeout
        )
        
        if result.returncode != 0:
            error_msg = f"FFmpeg failed with exit code {result.returncode}"
            logging.error(f"❌ {error_msg}")
            logging.error(f"📋 FFmpeg stderr: {result.stderr[:500]}")  # First 500 chars
            print(f"FFmpeg error: {result.stderr}", file=sys.stderr)
            return False
        
        logging.info(f"✅ FFmpeg completed successfully (exit code 0)")
        
        # Verify output file
        if not os.path.exists(output_wav):
            error_msg = f"Output WAV file was not created: {output_wav}"
            logging.error(f"❌ {error_msg}")
            return False
        
        wav_size = os.path.getsize(output_wav)
        logging.info(f"✅ WAV file created successfully")
        logging.info(f"📊 WAV file size: {wav_size:,} bytes ({wav_size / (1024*1024):.2f} MB)")
        
        # Calculate compression ratio
        compression_ratio = (1 - wav_size / file_size) * 100
        logging.info(f"📉 Size reduction: {compression_ratio:.1f}%")
        
        report_progress("converting", 50, "Audio extraction completed")
        logging.info("=" * 60)
        return True
        
    except subprocess.TimeoutExpired:
        error_msg = "FFmpeg timed out (exceeded 1 hour)"
        logging.error(f"⏱️ {error_msg}")
        print(error_msg, file=sys.stderr)
        return False
    except FileNotFoundError as e:
        logging.error(f"❌ File not found: {e}")
        print(f"File error: {str(e)}", file=sys.stderr)
        return False
    except Exception as e:
        error_msg = f"Audio extraction failed: {str(e)}"
        logging.error(f"❌ {error_msg}")
        logging.debug(f"📋 Stack trace:\n{traceback.format_exc()}")
        print(error_msg, file=sys.stderr)
        return False


def transcribe_audio(wav_file: str, output_file: str, config: Dict[str, Any]) -> bool:
    """Transcribe audio using Whisper AI"""
    try:
        logging.info("=" * 60)
        logging.info("🤖 STEP 2: TRANSCRIPTION WITH WHISPER AI")
        logging.info("=" * 60)
        logging.info(f"📁 Input WAV: {wav_file}")
        logging.info(f"📄 Output subtitle: {output_file}")
        
        # Import Whisper
        try:
            import whisper
            import torch
            logging.info("✅ Whisper and Torch imported successfully")
            logging.debug(f"🔍 Whisper version: {whisper.__version__ if hasattr(whisper, '__version__') else 'unknown'}")
            logging.debug(f"🔍 PyTorch version: {torch.__version__}")
        except ImportError as e:
            error_msg = f"Whisper import failed: {str(e)}. Please install: pip install openai-whisper torch"
            logging.error(f"❌ {error_msg}")
            print(error_msg, file=sys.stderr)
            return False
        
        # Validate WAV file
        if not os.path.exists(wav_file):
            error_msg = f"WAV file not found: {wav_file}"
            logging.error(f"❌ {error_msg}")
            raise FileNotFoundError(error_msg)
        
        wav_size = os.path.getsize(wav_file)
        logging.info(f"📊 WAV file size: {wav_size:,} bytes ({wav_size / (1024*1024):.2f} MB)")
        
        # Calculate estimated audio duration (16kHz mono, 16-bit)
        bytes_per_second = 16000 * 2  # 16kHz * 2 bytes per sample
        estimated_duration = wav_size / bytes_per_second
        logging.info(f"⏱️  Estimated audio duration: {estimated_duration:.2f} seconds ({estimated_duration/60:.2f} minutes)")
        
        report_progress("transcribing", 55, "Loading Whisper model...")
        
        # Load Whisper model
        model_name = config.get("whisper_model", "base")
        device = config.get("device", "cpu")
        
        logging.info(f"🔧 Configuration:")
        logging.info(f"   - Model: {model_name}")
        logging.info(f"   - Device: {device}")
        logging.info(f"⏳ Loading Whisper model (this may take a moment)...")
        
        # Check device availability
        if device == "cuda":
            if torch.cuda.is_available():
                logging.info(f"🎮 CUDA available: {torch.cuda.get_device_name(0)}")
                logging.info(f"💾 CUDA memory: {torch.cuda.get_device_properties(0).total_memory / 1024**3:.2f} GB")
            else:
                logging.warning("⚠️  CUDA requested but not available, falling back to CPU")
                device = "cpu"
        
        if device == "cpu":
            logging.info("💻 Using CPU for transcription")
        
        # Load model
        model_load_start = datetime.now()
        model = whisper.load_model(model_name, device=device)
        model_load_time = (datetime.now() - model_load_start).total_seconds()
        
        logging.info(f"✅ Model loaded successfully in {model_load_time:.2f} seconds")
        logging.info(f"📦 Model: {model_name}")
        
        report_progress("transcribing", 65, f"Model loaded: {model_name}")
        
        # Transcription options
        language = config.get("language", "English")
        original_language = language
        
        # Map language to Whisper language codes
        if language.lower() == "auto":
            language = None  # Auto-detect
        elif language.lower() == "english":
            language = "en"  # Force English
        elif language.lower() == "vietnamese":
            language = "vi"  # Force Vietnamese
        elif language.lower() == "chinese":
            language = "zh"
        elif language.lower() == "spanish":
            language = "es"
        elif language.lower() == "french":
            language = "fr"
        elif language.lower() == "german":
            language = "de"
        elif language.lower() == "japanese":
            language = "ja"
        elif language.lower() == "korean":
            language = "ko"
        # Add more languages as needed
        
        task = config.get("task", "transcribe")  # transcribe or translate
        fp16 = config.get("fp16", False) and device == "cuda"
        
        logging.info(f"🔧 Transcription settings:")
        logging.info(f"   - Language: {original_language} (code={language if language else 'auto-detect'})")
        logging.info(f"   - Task: {task}")
        logging.info(f"   - FP16: {fp16}")
        
        report_progress("transcribing", 70, "Starting transcription...")
        
        # Transcribe
        logging.info("🚀 Starting Whisper transcription...")
        start_time = datetime.now()
        result = model.transcribe(
            wav_file,
            language=language,
            task=task,
            fp16=fp16,
            verbose=False
        )
        duration = (datetime.now() - start_time).total_seconds()
        
        logging.info(f"✅ Transcription completed in {duration:.2f} seconds")
        
        # Log detected language
        detected_language = result.get("language", "unknown")
        logging.info(f"🌍 Detected language: {detected_language}")
        
        # Log segment count
        segments = result.get("segments", [])
        logging.info(f"📊 Generated {len(segments)} subtitle segments")
        
        # Log processing speed
        if estimated_duration > 0:
            speed_ratio = duration / estimated_duration
            logging.info(f"⚡ Processing speed: {speed_ratio:.2f}x realtime")
        
        # Log some sample segments
        if segments:
            logging.debug(f"📝 First segment: [{segments[0]['start']:.2f}s - {segments[0]['end']:.2f}s] {segments[0]['text'][:50]}...")
            if len(segments) > 1:
                logging.debug(f"📝 Last segment: [{segments[-1]['start']:.2f}s - {segments[-1]['end']:.2f}s] {segments[-1]['text'][:50]}...")
        
        report_progress("transcribing", 90, "Transcription completed, saving subtitle...")
        
        # Save subtitle file
        output_format = config.get("output_format", "srt").lower()
        logging.info(f"💾 Saving subtitle in format: {output_format}")
        
        save_subtitle(result, output_file, output_format)
        
        # Verify output file
        if not os.path.exists(output_file):
            error_msg = f"Subtitle file was not created: {output_file}"
            logging.error(f"❌ {error_msg}")
            return False
        
        subtitle_size = os.path.getsize(output_file)
        logging.info(f"✅ Subtitle file created successfully")
        logging.info(f"📊 Subtitle file size: {subtitle_size:,} bytes ({subtitle_size / 1024:.2f} KB)")
        
        # Calculate lines per minute
        if segments and estimated_duration > 0:
            lines_per_minute = (len(segments) / estimated_duration) * 60
            logging.info(f"📈 Average subtitle density: {lines_per_minute:.1f} lines/minute")
        
        report_progress("finalizing", 95, "Subtitle file created")
        logging.info("=" * 60)
        
        return True
        
    except FileNotFoundError as e:
        logging.error(f"❌ File not found: {e}")
        print(f"File error: {str(e)}", file=sys.stderr)
        return False
    except MemoryError as e:
        error_msg = f"Out of memory during transcription. Try using a smaller model (tiny/base)."
        logging.error(f"💾 {error_msg}")
        logging.info(f"💡 Suggestion: Current model '{config.get('whisper_model', 'base')}' may be too large for available memory")
        print(error_msg, file=sys.stderr)
        return False
    except Exception as e:
        error_msg = f"Transcription failed: {str(e)}"
        logging.error(f"❌ {error_msg}")
        logging.debug(f"📋 Stack trace:\n{traceback.format_exc()}")
        print(error_msg, file=sys.stderr)
        return False


def save_subtitle(result: Dict[str, Any], output_file: str, format: str):
    """Save transcription result in specified format"""
    try:
        logging.debug(f"💾 Saving subtitle as {format}: {output_file}")
        
        if format == "srt":
            save_srt(result["segments"], output_file)
        elif format == "vtt":
            save_vtt(result["segments"], output_file)
        elif format == "txt":
            save_txt(result["text"], output_file)
        elif format == "json":
            save_json(result, output_file)
        else:
            logging.warning(f"⚠️  Unknown format '{format}', defaulting to SRT")
            save_srt(result["segments"], output_file)
        
        logging.debug(f"✅ Subtitle saved successfully: {output_file}")
        
    except Exception as e:
        error_msg = f"Failed to save subtitle: {str(e)}"
        logging.error(error_msg)
        logging.debug(traceback.format_exc())
        raise


def save_srt(segments: list, output_file: str):
    """Save as SRT format"""
    try:
        with open(output_file, "w", encoding="utf-8") as f:
            for i, segment in enumerate(segments, start=1):
                start = format_timestamp(segment["start"])
                end = format_timestamp(segment["end"])
                text = segment["text"].strip()
                
                f.write(f"{i}\n")
                f.write(f"{start} --> {end}\n")
                f.write(f"{text}\n\n")
        
        logging.debug(f"Saved {len(segments)} segments to SRT file")
    except Exception as e:
        logging.error(f"Failed to save SRT file: {e}")
        raise


def save_vtt(segments: list, output_file: str):
    """Save as WebVTT format"""
    try:
        with open(output_file, "w", encoding="utf-8") as f:
            f.write("WEBVTT\n\n")
            
            for segment in segments:
                start = format_timestamp(segment["start"], vtt=True)
                end = format_timestamp(segment["end"], vtt=True)
                text = segment["text"].strip()
                
                f.write(f"{start} --> {end}\n")
                f.write(f"{text}\n\n")
        
        logging.debug(f"Saved {len(segments)} segments to VTT file")
    except Exception as e:
        logging.error(f"Failed to save VTT file: {e}")
        raise


def save_txt(text: str, output_file: str):
    """Save as plain text"""
    try:
        with open(output_file, "w", encoding="utf-8") as f:
            f.write(text.strip())
        
        logging.debug(f"Saved text transcript ({len(text)} chars)")
    except Exception as e:
        logging.error(f"Failed to save TXT file: {e}")
        raise


def save_json(result: Dict[str, Any], output_file: str):
    """Save full result as JSON"""
    try:
        with open(output_file, "w", encoding="utf-8") as f:
            json.dump(result, f, ensure_ascii=False, indent=2)
        
        logging.debug(f"Saved full JSON result")
    except Exception as e:
        logging.error(f"Failed to save JSON file: {e}")
        raise


def format_timestamp(seconds: float, vtt: bool = False) -> str:
    """Format timestamp for SRT/VTT"""
    hours = int(seconds // 3600)
    minutes = int((seconds % 3600) // 60)
    secs = int(seconds % 60)
    millis = int((seconds % 1) * 1000)
    
    if vtt:
        return f"{hours:02d}:{minutes:02d}:{secs:02d}.{millis:03d}"
    else:
        return f"{hours:02d}:{minutes:02d}:{secs:02d},{millis:03d}"


def process_media_file(config: Dict[str, Any]) -> bool:
    """Main processing function"""
    start_time = datetime.now()
    
    try:
        logging.info("=" * 70)
        logging.info("🎬 VIDEO SUBTITLE GENERATOR - PROCESSING START")
        logging.info("=" * 70)
        
        # Apply default configuration values
        if CONFIG_MODULE_AVAILABLE:
            logging.info("🔧 Applying default configuration values...")
            config = apply_defaults(config)
            
            # Validate configuration
            logging.info("✅ Validating configuration...")
            is_valid, error_msg = validate_config(config)
            if not is_valid:
                logging.error(f"❌ Configuration validation failed: {error_msg}")
                report_result(False, error=error_msg)
                return False
            logging.info("✅ Configuration validated successfully")
        
        # Validate required config
        required_keys = ["input_file", "output_dir"]
        for key in required_keys:
            if key not in config:
                error_msg = f"Missing required config: {key}"
                logging.error(f"❌ {error_msg}")
                raise KeyError(key)
        
        input_file = config["input_file"]
        output_dir = config["output_dir"]
        
        logging.info("")
        logging.info("📋 PROCESSING CONFIGURATION")
        logging.info("-" * 70)
        logging.info(f"📁 Input file: {input_file}")
        logging.info(f"📂 Output directory: {output_dir}")
        logging.info(f"🤖 Whisper model: {config.get('whisper_model', 'base')}")
        logging.info(f"🌍 Language: {config.get('language', 'auto')}")
        logging.info(f"💻 Device: {config.get('device', 'cpu')}")
        logging.info(f"📄 Output format: {config.get('output_format', 'srt')}")
        logging.info(f"🔧 Task: {config.get('task', 'transcribe')}")
        logging.info("-" * 70)
        logging.debug(f"Full configuration: {json.dumps(config, indent=2)}")
        
        # Create output directory
        logging.info("")
        logging.info(f"📂 Creating output directory: {output_dir}")
        os.makedirs(output_dir, exist_ok=True)
        logging.info(f"✅ Output directory ready: {output_dir}")
        
        # Generate output filenames
        base_name = Path(input_file).stem
        wav_file = os.path.join(output_dir, f"{base_name}.wav")
        
        output_format = config.get("output_format", "srt")
        subtitle_file = os.path.join(output_dir, f"{base_name}.{output_format}")
        
        logging.info("")
        logging.info("📝 OUTPUT FILES")
        logging.info(f"   🔊 WAV: {wav_file}")
        logging.info(f"   📄 Subtitle: {subtitle_file}")
        
        report_progress("queued", 0, f"Processing: {Path(input_file).name}")
        
        # Step 1: Extract audio
        logging.info("")
        if not extract_audio(input_file, wav_file, config.get("ffmpeg_path", "ffmpeg")):
            error_msg = "Audio extraction failed"
            logging.error(f"❌ {error_msg}")
            report_result(False, error=error_msg)
            return False
        
        # Step 2: Transcribe audio
        logging.info("")
        if not transcribe_audio(wav_file, subtitle_file, config):
            error_msg = "Transcription failed"
            logging.error(f"❌ {error_msg}")
            report_result(False, wav_file=wav_file, error=error_msg)
            return False
        
        # Success
        duration = (datetime.now() - start_time).total_seconds()
        
        logging.info("")
        logging.info("=" * 70)
        logging.info("✅ PROCESSING COMPLETED SUCCESSFULLY")
        logging.info("=" * 70)
        
        report_progress("completed", 100, "Processing completed successfully")
        
        # Get file sizes
        input_size = os.path.getsize(input_file)
        wav_size = os.path.getsize(wav_file)
        subtitle_size = os.path.getsize(subtitle_file)
        
        metadata = {
            "input_size": str(input_size),
            "wav_size": str(wav_size),
            "subtitle_size": str(subtitle_size),
            "duration_seconds": f"{duration:.2f}",
            "base_name": base_name
        }
        
        logging.info("📊 FINAL STATISTICS")
        logging.info("-" * 70)
        logging.info(f"⏱️  Total processing time: {duration:.2f} seconds ({duration/60:.2f} minutes)")
        logging.info(f"📁 Input video size: {input_size:,} bytes ({input_size / (1024*1024):.2f} MB)")
        logging.info(f"🔊 WAV audio size: {wav_size:,} bytes ({wav_size / (1024*1024):.2f} MB)")
        logging.info(f"📄 Subtitle file size: {subtitle_size:,} bytes ({subtitle_size / 1024:.2f} KB)")
        logging.info(f"📝 Base name: {base_name}")
        logging.info("-" * 70)
        
        report_result(True, wav_file=wav_file, subtitle_file=subtitle_file, metadata=metadata)
        
        logging.info("🎉 All done! Subtitle file ready for use.")
        logging.info("=" * 70)
        
        return True
        
    except KeyError as e:
        error_msg = f"Missing required config: {str(e)}"
        logging.error(f"❌ {error_msg}")
        logging.debug(f"📋 Stack trace:\n{traceback.format_exc()}")
        print(error_msg, file=sys.stderr)
        report_result(False, error=error_msg)
        return False
    except FileNotFoundError as e:
        error_msg = f"File not found: {str(e)}"
        logging.error(f"❌ {error_msg}")
        logging.debug(f"📋 Stack trace:\n{traceback.format_exc()}")
        print(error_msg, file=sys.stderr)
        report_result(False, error=error_msg)
        return False
    except PermissionError as e:
        error_msg = f"Permission denied: {str(e)}"
        logging.error(f"❌ {error_msg}")
        logging.info(f"💡 Suggestion: Check file/folder permissions and ensure they are not read-only")
        logging.debug(f"📋 Stack trace:\n{traceback.format_exc()}")
        print(error_msg, file=sys.stderr)
        report_result(False, error=error_msg)
        return False
    except Exception as e:
        error_msg = f"Processing failed: {str(e)}"
        logging.error(f"❌ {error_msg}")
        logging.debug(f"📋 Stack trace:\n{traceback.format_exc()}")
        print(error_msg, file=sys.stderr)
        report_result(False, error=error_msg)
        return False
    finally:
        duration = (datetime.now() - start_time).total_seconds()
        logging.info("")
        logging.info(f"⏱️  Total execution time: {duration:.2f} seconds ({duration/60:.2f} minutes)")
        logging.info(f"📅 Finished at: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")


def main():
    """Entry point"""
    parser = argparse.ArgumentParser(description="Process media file for subtitle generation")
    parser.add_argument("--config", required=True, help="Base64 encoded JSON configuration")
    parser.add_argument("--log-dir", default="logs", help="Directory for log files")
    parser.add_argument("--verbose", action="store_true", help="Enable verbose logging")
    
    args = parser.parse_args()
    
    try:
        # Setup logging
        setup_logging(args.log_dir)
        
        if args.verbose:
            logging.getLogger().setLevel(logging.DEBUG)
            logging.info("🔍 Verbose logging enabled")
        
        logging.info("=" * 70)
        logging.info("🎬 VIDEO SUBTITLE GENERATOR - PYTHON WORKER")
        logging.info("=" * 70)
        logging.info(f"🐍 Python version: {sys.version.split()[0]}")
        logging.info(f"📂 Working directory: {os.getcwd()}")
        logging.info(f"📅 Started at: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")
        logging.info("=" * 70)
        
        # Decode configuration
        try:
            logging.info("🔓 Decoding configuration...")
            config_json = base64.b64decode(args.config).decode("utf-8")
            config = json.loads(config_json)
            logging.info("✅ Configuration decoded successfully")
            logging.debug(f"📋 Configuration keys: {list(config.keys())}")
        except Exception as e:
            error_msg = f"Failed to decode configuration: {str(e)}"
            logging.error(f"❌ {error_msg}")
            logging.debug(f"📋 Stack trace:\n{traceback.format_exc()}")
            print(error_msg, file=sys.stderr)
            report_result(False, error=error_msg)
            sys.exit(1)
        
        # Process the file
        success = process_media_file(config)
        
        exit_code = 0 if success else 1
        
        logging.info("")
        logging.info("=" * 70)
        if success:
            logging.info(f"✅ Process completed successfully (exit code: {exit_code})")
        else:
            logging.info(f"❌ Process failed (exit code: {exit_code})")
        logging.info("=" * 70)
        
        sys.exit(exit_code)
        
    except KeyboardInterrupt:
        logging.warning("")
        logging.warning("=" * 70)
        logging.warning("⚠️  Process interrupted by user (Ctrl+C)")
        logging.warning("=" * 70)
        print("Process interrupted", file=sys.stderr)
        sys.exit(2)
    except Exception as e:
        error_msg = f"Fatal error: {str(e)}"
        logging.critical("")
        logging.critical("=" * 70)
        logging.critical(f"💥 FATAL ERROR: {error_msg}")
        logging.critical("=" * 70)
        logging.debug(f"📋 Stack trace:\n{traceback.format_exc()}")
        print(error_msg, file=sys.stderr)
        report_result(False, error=error_msg)
        sys.exit(1)


if __name__ == "__main__":
    main()
