using System;
using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using VideoSubtitleGenerator.UI.Wpf.ViewModels;

namespace VideoSubtitleGenerator.UI.Wpf;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("=== MainWindow Constructor START ===");
            
            InitializeComponent();
            System.Diagnostics.Debug.WriteLine("=== InitializeComponent() completed ===");
            
            // Get ViewModel from DI (Service Locator pattern)
            if (App.ServiceProvider == null)
            {
                System.Diagnostics.Debug.WriteLine("✗ ServiceProvider is NULL!");
                throw new InvalidOperationException("ServiceProvider is not initialized!");
            }
            
            System.Diagnostics.Debug.WriteLine("=== Getting MainViewModel from DI ===");
            _viewModel = App.ServiceProvider.GetRequiredService<MainViewModel>();
            System.Diagnostics.Debug.WriteLine($"✓ MainViewModel created: {_viewModel != null}");
            
            DataContext = _viewModel;
            System.Diagnostics.Debug.WriteLine("✓ DataContext set");
            System.Diagnostics.Debug.WriteLine($"✓ Jobs count: {_viewModel.Jobs.Count}");
            System.Diagnostics.Debug.WriteLine($"✓ Log entries count: {_viewModel.LogEntries.Count}");
            
            // Wire up button click events
            btnSettings.Click += BtnSettings_Click;
            System.Diagnostics.Debug.WriteLine("✓ Event handlers wired");
            
            System.Diagnostics.Debug.WriteLine("=== MainWindow Constructor END (SUCCESS) ===");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("=== MainWindow Constructor FAILED ===");
            System.Diagnostics.Debug.WriteLine($"✗ Error: {ex.GetType().Name}");
            System.Diagnostics.Debug.WriteLine($"✗ Message: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"✗ Stack trace:\n{ex.StackTrace}");
            
            if (ex.InnerException != null)
            {
                System.Diagnostics.Debug.WriteLine($"✗ Inner exception: {ex.InnerException.Message}");
                System.Diagnostics.Debug.WriteLine($"✗ Inner stack:\n{ex.InnerException.StackTrace}");
            }
            
            // Show error to user
            MessageBox.Show(
                $"Failed to initialize MainWindow:\n\n" +
                $"Error: {ex.Message}\n\n" +
                $"Inner: {ex.InnerException?.Message}\n\n" +
                $"Please check the Output window for details.", 
                "Initialization Error", 
                MessageBoxButton.OK, 
                MessageBoxImage.Error);
            
            // Rethrow to prevent window from showing
            throw;
        }
    }

    private void BtnSettings_Click(object sender, RoutedEventArgs e)
    {
        // Use ViewModel's OpenSettings method which passes ISettingsService
        _viewModel.OpenSettings();
    }

    private void BtnStart_Click(object sender, RoutedEventArgs e)
    {
        // Get actual file count from ViewModel
        int fileCount = _viewModel.Jobs.Count;
        
        if (fileCount == 0)
        {
            MessageBox.Show("Please add video files first.", "No Files", 
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        
        var dialog = new ProcessingModeDialog(fileCount)
        {
            Owner = this
        };
        
        bool? result = dialog.ShowDialog();
        
        if (result == true)
        {
            // Execute StartProcessingCommand
            if (_viewModel.StartProcessingCommand.CanExecute(null))
            {
                _viewModel.StartProcessingCommand.Execute(null);
            }
        }
    }

    private void JobsDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        // Get selected job from ViewModel
        if (jobsDataGrid.SelectedItem is TranscriptionJobViewModel selectedJob)
        {
            var dialog = new JobDetailsDialog(selectedJob.FileName, selectedJob.OutputDirectory)
            {
                Owner = this
            };
            dialog.ShowDialog();
            
            _viewModel.AddLog($"Viewed details for: {selectedJob.FileName}");
        }
    }
}