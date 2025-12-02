using System.Configuration;
using System.Data;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using VideoSubtitleGenerator.Core;
using VideoSubtitleGenerator.Core.Interfaces;
using VideoSubtitleGenerator.Infrastructure.Services;
using VideoSubtitleGenerator.UI.Wpf.ViewModels;

namespace VideoSubtitleGenerator.UI.Wpf;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public static ServiceProvider? ServiceProvider { get; private set; }

    public App()
    {
        // Configure DI container
        var services = new ServiceCollection();
        ConfigureServices(services);
        ServiceProvider = services.BuildServiceProvider();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            System.Diagnostics.Debug.WriteLine("=== App.OnStartup START ===");
            System.Diagnostics.Debug.WriteLine($"ServiceProvider is null: {ServiceProvider == null}");
            
            // Resolve MainWindow from DI container
            System.Diagnostics.Debug.WriteLine("Attempting to resolve MainWindow...");
            var mainWindow = ServiceProvider?.GetRequiredService<MainWindow>();
            System.Diagnostics.Debug.WriteLine($"MainWindow resolved: {mainWindow != null}");
            
            if (mainWindow != null)
            {
                System.Diagnostics.Debug.WriteLine("Showing MainWindow...");
                mainWindow.Show();
                System.Diagnostics.Debug.WriteLine("=== App.OnStartup SUCCESS ===");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("✗ ServiceProvider returned null!");
                MessageBox.Show(
                    "Failed to initialize application: ServiceProvider is null",
                    "Startup Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Shutdown(1);
            }
        }
        catch (Exception ex)
        {
            Utilities.WriteToLog(ex);
            System.Diagnostics.Debug.WriteLine("=== App.OnStartup EXCEPTION ===");
            System.Diagnostics.Debug.WriteLine($"Exception Type: {ex.GetType().FullName}");
            System.Diagnostics.Debug.WriteLine($"Message: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack Trace:\n{ex.StackTrace}");
            
            if (ex.InnerException != null)
            {
                System.Diagnostics.Debug.WriteLine($"\nInner Exception: {ex.InnerException.GetType().FullName}");
                System.Diagnostics.Debug.WriteLine($"Inner Message: {ex.InnerException.Message}");
                System.Diagnostics.Debug.WriteLine($"Inner Stack:\n{ex.InnerException.StackTrace}");
            }
            
            MessageBox.Show(
                $"Failed to start application:\n\n" +
                $"Error: {ex.GetType().Name}\n" +
                $"Message: {ex.Message}\n\n" +
                $"Inner: {ex.InnerException?.Message}\n\n" +
                $"See Output window for full stack trace.",
                "Startup Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Register Core Services (Singletons - shared across app)
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<ILogService, FileLogService>();
        services.AddSingleton<IPythonWorkerService, PythonWorkerService>();
        services.AddSingleton<IFFmpegService, FFmpegService>();
        services.AddSingleton<IJobQueueService, JobQueueService>();
        services.AddSingleton<IJobOrchestrator, JobOrchestrator>();
        
        // Register ViewModels (Transients - new instance each time)
        services.AddTransient<MainViewModel>();
        services.AddTransient<SettingsViewModel>();
        
        // Register Windows (Transients)
        services.AddTransient<MainWindow>();
        services.AddTransient<SettingsWindow>();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        // Cleanup
        ServiceProvider?.Dispose();
        base.OnExit(e);
    }
}


