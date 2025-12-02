using System.Diagnostics;
using System.IO;
using System.Windows;
using VideoSubtitleGenerator.Core;

namespace VideoSubtitleGenerator.UI.Wpf;

/// <summary>
/// Interaction logic for JobDetailsDialog.xaml
/// </summary>
public partial class JobDetailsDialog : Window
{
    private readonly string _outputFolderPath;

    public JobDetailsDialog(string jobFileName, string outputFolder = @"C:\Output")
    {
        InitializeComponent();
        
        _outputFolderPath = outputFolder;
        
        // Set window title
        Title = $"Job Details - {jobFileName}";
        txtJobTitle.Text = $"Job Details - {jobFileName}";
        
        // TODO: Load actual job data from TranscriptionJob object
        // For now, using mock data from mockup
        LoadMockData(jobFileName);
    }

    private void LoadMockData(string fileName)
    {
        // File Information (from mockup)
        txtFileName.Text = "Module.9.3 Information Security Management";
        txtFilePath.Text = @"C:\TRY_CONVERT\video_file\Module.9.3.mpeg";
        txtFileSize.Text = "245 MB";
        txtDuration.Text = "23:45";
        txtFormat.Text = "MPEG";
        
        // Processing Details (from mockup)
        txtStatus.Text = "Transcribing";
        txtProgress.Text = "73%";
        progressFill.Width = 219; // 73% of 300px
        txtStartTime.Text = "14:23:45";
        txtElapsed.Text = "05:34";
        txtEstRemaining.Text = "02:15";
        txtCurrentPhase.Text = "Whisper AI Transcription";
        txtModel.Text = "small";
        txtLanguage.Text = "English";
        
        // Output Files
        txtWavPath.Text = $@"{_outputFolderPath}\Module.9.3.wav";
        txtSrtPath.Text = $@"{_outputFolderPath}\Module.9.3.srt";
        
        // Detailed Log (from mockup)
        txtDetailedLog.Text = @"[14:23:45] Job started
                            [14:23:46] Converting to WAV format...
                            [14:24:12] WAV conversion complete
                            [14:24:13] Loading Whisper model (small)...
                            [14:24:18] Model loaded successfully
                            [14:24:18] Transcribing audio...
                            [14:25:00] Processing segment 1/10...
                            [14:25:30] Processing segment 2/10...
                            [14:26:00] Processing segment 3/10...
                            [14:26:30] Processing segment 4/10...
                            [14:27:00] Processing segment 5/10...
                            [14:27:30] Processing segment 6/10...
                            [14:28:00] Processing segment 7/10... (current)";
    }

    private void OpenOutputFolder_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (Directory.Exists(_outputFolderPath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = _outputFolderPath,
                    UseShellExecute = true,
                    Verb = "open"
                });
            }
            else
            {
                MessageBox.Show(
                    $"Output folder does not exist:\n{_outputFolderPath}", 
                    "Folder Not Found", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Warning);
            }
        }
        catch (Exception ex)
        {
            Utilities.WriteToLog(ex);
            MessageBox.Show(
                $"Failed to open output folder:\n{ex.Message}", 
                "Error", 
                MessageBoxButton.OK, 
                MessageBoxImage.Error);
        }
    }

    private void Retry_Click(object sender, RoutedEventArgs e)
    {
        // TODO: Implement retry logic
        MessageBox.Show(
            "Retry functionality will be implemented with ViewModels", 
            "Retry Job", 
            MessageBoxButton.OK, 
            MessageBoxImage.Information);
        
        DialogResult = true;
        Close();
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
