#!/usr/bin/env python3
"""
Test script for process_media.py
Creates a test configuration and runs the worker
"""

import json
import base64
import subprocess
import sys
import os
from pathlib import Path

def test_worker():
    """Test the worker with a sample configuration"""
    
    # Test configuration
    config = {
        "input_file": "C:\\Videos\\test.mp4",  # Change to your test file
        "output_dir": "C:\\Output\\test",
        "ffmpeg_path": "ffmpeg",
        "whisper_model": "tiny",  # Use smallest model for testing
        "language": "English",
        "device": "cpu",
        "fp16": False,
        "task": "transcribe",
        "output_format": "srt"
    }
    
    print("=" * 50)
    print("Testing Python Worker")
    print("=" * 50)
    print()
    
    # Check if input file exists
    if not os.path.exists(config["input_file"]):
        print(f"[ERROR] Input file not found: {config['input_file']}")
        print("Please update the 'input_file' path in this script to a valid video file.")
        return False
    
    print(f"Input file: {config['input_file']}")
    print(f"Output dir: {config['output_dir']}")
    print(f"Model: {config['whisper_model']}")
    print()
    
    # Encode configuration
    config_json = json.dumps(config)
    config_base64 = base64.b64encode(config_json.encode()).decode()
    
    # Run worker
    cmd = [sys.executable, "process_media.py", "--config", config_base64]
    
    print("Running worker...")
    print()
    
    try:
        result = subprocess.run(
            cmd,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            text=True,
            encoding='utf-8',
            errors='replace'
        )
        
        print("STDOUT:")
        print("-" * 50)
        print(result.stdout)
        print()
        
        if result.stderr:
            print("STDERR:")
            print("-" * 50)
            print(result.stderr)
            print()
        
        if result.returncode == 0:
            print("[SUCCESS] Worker completed successfully!")
            return True
        else:
            print(f"[ERROR] Worker failed with exit code: {result.returncode}")
            return False
            
    except Exception as e:
        print(f"[ERROR] Failed to run worker: {e}")
        return False

if __name__ == "__main__":
    success = test_worker()
    sys.exit(0 if success else 1)
