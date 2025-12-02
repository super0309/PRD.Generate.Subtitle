@echo off
REM Setup script for Python Worker environment

echo ========================================
echo Video Subtitle Generator - Python Setup
echo ========================================
echo.

REM Check Python installation
python --version >nul 2>&1
if errorlevel 1 (
    echo [ERROR] Python not found! Please install Python 3.8 or higher.
    echo Download from: https://www.python.org/downloads/
    pause
    exit /b 1
)

echo [OK] Python found:
python --version
echo.

REM Check FFmpeg installation
ffmpeg -version >nul 2>&1
if errorlevel 1 (
    echo [WARNING] FFmpeg not found in PATH!
    echo Please install FFmpeg: https://ffmpeg.org/download.html
    echo Or install via Chocolatey: choco install ffmpeg
    echo.
)

echo [OK] FFmpeg found
echo.

REM Create virtual environment (optional but recommended)
echo Creating virtual environment...
python -m venv venv

echo.
echo Activating virtual environment...
call venv\Scripts\activate.bat

echo.
echo Installing Python dependencies...
pip install --upgrade pip
pip install -r requirements.txt

echo.
echo ========================================
echo Setup completed!
echo ========================================
echo.
echo To activate the environment in future:
echo   venv\Scripts\activate.bat
echo.
echo To test the worker:
echo   python process_media.py --help
echo.
pause
