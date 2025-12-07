# Test Python Worker Call
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Testing Python Worker Progress File" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# Paths
$scriptPath = "c:\Project\PRD.Generate.Subttitle\src\VideoSubtitleGenerator.UI.Wpf\bin\Debug\net6.0-windows\python-worker\process_media.py"
$outputDir = "c:\Project\PRD.Generate.Subttitle\src\VideoSubtitleGenerator.UI.Wpf\bin\Debug\net6.0-windows\python-worker"

# Test Job ID
$testJobId = [guid]::NewGuid().ToString()
Write-Host "`nTest Job ID: $testJobId" -ForegroundColor Yellow

# Config
$config = @{
    job_id = $testJobId
    input_file = "C:\test_video.mp4"  # Fake file
    output_dir = "C:\output"
    ffmpeg_path = "ffmpeg"
    whisper_model = "tiny"
    language = "English"
    device = "cpu"
    fp16 = $false
    task = "transcribe"
    output_format = "srt"
} | ConvertTo-Json -Compress

# Encode to Base64
$configBytes = [System.Text.Encoding]::UTF8.GetBytes($config)
$configBase64 = [Convert]::ToBase64String($configBytes)

Write-Host "`nConfig encoded (first 50 chars): $($configBase64.Substring(0, [Math]::Min(50, $configBase64.Length)))..." -ForegroundColor Gray

# Expected progress file path
$progressFile = Join-Path $outputDir "$testJobId`_progress.json"
Write-Host "`nExpected progress file: $progressFile" -ForegroundColor Yellow

# Run Python (will fail because no input file, but should create progress file)
Write-Host "`nRunning Python worker..." -ForegroundColor Cyan
$pythonArgs = "--config", $configBase64, "--log-dir", (Join-Path $outputDir "logs")

Write-Host "Command: python `"$scriptPath`" $($pythonArgs -join ' ')" -ForegroundColor Gray

try {
    $process = Start-Process -FilePath "python" `
        -ArgumentList "`"$scriptPath`"", $pythonArgs `
        -NoNewWindow `
        -Wait `
        -PassThru `
        -RedirectStandardOutput "python_stdout.txt" `
        -RedirectStandardError "python_stderr.txt"
    
    Write-Host "`nPython exit code: $($process.ExitCode)" -ForegroundColor $(if($process.ExitCode -eq 0){"Green"}else{"Red"})
    
    # Check if progress file was created
    Start-Sleep -Milliseconds 500
    
    if (Test-Path $progressFile) {
        Write-Host "`n✅ SUCCESS: Progress file was created!" -ForegroundColor Green
        Write-Host "`nProgress file content:" -ForegroundColor Cyan
        Get-Content $progressFile | ConvertFrom-Json | ConvertTo-Json -Depth 10
        
        # Cleanup
        Remove-Item $progressFile -ErrorAction SilentlyContinue
    } else {
        Write-Host "`n❌ FAILED: Progress file was NOT created!" -ForegroundColor Red
        Write-Host "`nExpected at: $progressFile" -ForegroundColor Yellow
    }
    
    # Show output
    if (Test-Path "python_stdout.txt") {
        Write-Host "`n--- STDOUT ---" -ForegroundColor Cyan
        Get-Content "python_stdout.txt" | Select-Object -Last 20
    }
    
    if (Test-Path "python_stderr.txt") {
        Write-Host "`n--- STDERR ---" -ForegroundColor Cyan
        Get-Content "python_stderr.txt" | Select-Object -Last 20
    }
    
} catch {
    Write-Host "`n❌ Error running Python: $_" -ForegroundColor Red
} finally {
    # Cleanup temp files
    Remove-Item "python_stdout.txt" -ErrorAction SilentlyContinue
    Remove-Item "python_stderr.txt" -ErrorAction SilentlyContinue
}

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "Test completed" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
