using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;
using VideoSubtitleGenerator.Core.Interfaces;
using VideoSubtitleGenerator.UI.Wpf.ViewModels;

namespace VideoSubtitleGenerator.UI.Wpf;

/// <summary>
/// Interaction logic for SettingsWindow.xaml
/// </summary>
public partial class SettingsWindow : Window
{
    private readonly SettingsViewModel _viewModel;

    public SettingsWindow(ISettingsService settingsService)
    {
        InitializeComponent();
        
        // Initialize ViewModel with injected service
        _viewModel = new SettingsViewModel(settingsService);
        DataContext = _viewModel;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        // TODO: Save settings
        MessageBox.Show("Settings saved successfully!", "Settings", MessageBoxButton.OK, MessageBoxImage.Information);
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        // Open URL in default browser
        Process.Start(new ProcessStartInfo
        {
            FileName = e.Uri.AbsoluteUri,
            UseShellExecute = true
        });
        e.Handled = true;
    }
}
