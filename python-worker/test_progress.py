#!/usr/bin/env python3
"""
Test script to verify progress file creation
"""

import sys
import os
import json
import base64
import uuid
from datetime import datetime

# Add current directory to path
sys.path.insert(0, os.path.dirname(__file__))

# Test configuration
test_job_id = str(uuid.uuid4())
print(f"Test Job ID: {test_job_id}")

config = {
    "job_id": test_job_id,
    "input_file": "C:\\test.mp4",  # Fake file for testing
    "output_dir": "C:\\output",
    "ffmpeg_path": "ffmpeg",
    "whisper_model": "tiny",
    "language": "English",
    "device": "cpu",
    "fp16": False,
    "task": "transcribe",
    "output_format": "srt"
}

# Encode config
config_json = json.dumps(config)
config_base64 = base64.b64encode(config_json.encode()).decode()

print(f"\nConfig (Base64): {config_base64[:50]}...")
print(f"\nExpected progress file location:")

# Simulate what process_media.py does
script_dir = os.path.dirname(os.path.abspath(__file__))
progress_file = os.path.join(script_dir, f"{test_job_id}_progress.json")
print(f"  {progress_file}")

# Create a test progress file
progress = {
    "phase": "testing",
    "percent": 50,
    "message": "Test progress",
    "timestamp": datetime.now().isoformat()
}

try:
    with open(progress_file, 'w', encoding='utf-8') as f:
        json.dump(progress, f, ensure_ascii=False, indent=2)
    print(f"\n✅ Successfully created progress file")
    print(f"   File exists: {os.path.exists(progress_file)}")
    
    # Read it back
    with open(progress_file, 'r', encoding='utf-8') as f:
        read_back = json.load(f)
    print(f"   Content verified: {read_back}")
    
    # Clean up
    os.remove(progress_file)
    print(f"\n✅ Test completed successfully!")
    
except Exception as e:
    print(f"\n❌ Error: {e}")
    import traceback
    traceback.print_exc()
