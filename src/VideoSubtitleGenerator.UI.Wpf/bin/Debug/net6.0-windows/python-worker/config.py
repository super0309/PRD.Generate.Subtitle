"""
Python Worker Configuration Module
Handles configuration validation and default values
"""

import os
from typing import Dict, Any, Optional

# Default configurations
DEFAULT_CONFIG = {
    "ffmpeg_path": "ffmpeg",
    "whisper_model": "base",
    "language": "English",
    "device": "cpu",
    "fp16": False,
    "task": "transcribe",
    "output_format": "srt"
}

# Valid values
VALID_MODELS = ["tiny", "base", "small", "medium", "large"]
VALID_FORMATS = ["srt", "vtt", "txt", "json"]
VALID_TASKS = ["transcribe", "translate"]
VALID_DEVICES = ["cpu", "cuda"]

# Language mappings
LANGUAGE_CODES = {
    "english": "en",
    "vietnamese": "vi",
    "chinese": "zh",
    "japanese": "ja",
    "korean": "ko",
    "french": "fr",
    "german": "de",
    "spanish": "es",
    "auto": None  # Auto-detect
}


def validate_config(config: Dict[str, Any]) -> tuple[bool, Optional[str]]:
    """
    Validate configuration parameters
    Returns: (is_valid, error_message)
    """
    
    # Check required fields
    if "input_file" not in config:
        return False, "Missing required field: input_file"
    
    if "output_dir" not in config:
        return False, "Missing required field: output_dir"
    
    # Check input file exists
    if not os.path.exists(config["input_file"]):
        return False, f"Input file not found: {config['input_file']}"
    
    # Validate model
    model = config.get("whisper_model", "base")
    if model not in VALID_MODELS:
        return False, f"Invalid model: {model}. Must be one of {VALID_MODELS}"
    
    # Validate format
    output_format = config.get("output_format", "srt")
    if output_format not in VALID_FORMATS:
        return False, f"Invalid format: {output_format}. Must be one of {VALID_FORMATS}"
    
    # Validate task
    task = config.get("task", "transcribe")
    if task not in VALID_TASKS:
        return False, f"Invalid task: {task}. Must be one of {VALID_TASKS}"
    
    # Validate device
    device = config.get("device", "cpu")
    if device not in VALID_DEVICES:
        return False, f"Invalid device: {device}. Must be one of {VALID_DEVICES}"
    
    return True, None


def apply_defaults(config: Dict[str, Any]) -> Dict[str, Any]:
    """Apply default values to missing config keys"""
    result = DEFAULT_CONFIG.copy()
    result.update(config)
    return result


def get_language_code(language: str) -> Optional[str]:
    """Convert language name to Whisper language code"""
    lang_lower = language.lower()
    return LANGUAGE_CODES.get(lang_lower, None)
