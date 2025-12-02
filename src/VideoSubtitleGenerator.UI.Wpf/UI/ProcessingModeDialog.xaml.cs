using System.Windows;

namespace VideoSubtitleGenerator.UI.Wpf;

/// <summary>
/// Interaction logic for ProcessingModeDialog.xaml
/// </summary>
public partial class ProcessingModeDialog : Window
{
    public bool IsSequentialMode { get; private set; }
    public int MaxParallelJobs { get; private set; }
    public bool RememberChoice { get; private set; }

    public ProcessingModeDialog(int fileCount)
    {
        InitializeComponent();
        
        // Update description with file count
        txtDescription.Text = $"Bạn sắp xử lý {fileCount} tệp video. Vui lòng chọn chế độ xử lý bạn muốn sử dụng.";
        
        // Default values
        IsSequentialMode = true;
        MaxParallelJobs = 2;
        RememberChoice = false;
    }

    private void StartButton_Click(object sender, RoutedEventArgs e)
    {
        // Get selected mode
        IsSequentialMode = rbSequential.IsChecked == true;
        
        // Get max parallel jobs if parallel mode
        if (!IsSequentialMode)
        {
            MaxParallelJobs = int.Parse(((System.Windows.Controls.ComboBoxItem)cmbMaxParallelJobs.SelectedItem).Content.ToString()!);
        }
        
        // Get remember choice
        RememberChoice = chkRememberChoice.IsChecked == true;
        
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
