using System.Configuration;
using System.Data;
using System.IO;
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
        try
        {
            // Handle unhandled exceptions
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            
            // Configure DI container
            var services = new ServiceCollection();
            ConfigureServices(services);
            ServiceProvider = services.BuildServiceProvider();
            
            File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app_constructor_success.log"), 
                $"App constructor completed successfully at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        }
        catch (Exception ex)
        {
            Utilities.WriteToLog(ex);
            var errorMsg = $"App Constructor Exception:\n\n" +
                           $"Type: {ex.GetType().Name}\n" +
                           $"Message: {ex.Message}\n\n" +
                           $"Inner: {ex.InnerException?.Message}\n\n" +
                           $"Stack:\n{ex.StackTrace}";
            
            File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash_constructor.log"), errorMsg);
            MessageBox.Show(errorMsg, "Constructor Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Environment.Exit(1);
        }
    }
    
    private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        var errorMsg = $"Unhandled UI Exception:\n\n" +
                       $"Type: {e.Exception.GetType().Name}\n" +
                       $"Message: {e.Exception.Message}\n\n" +
                       $"Inner: {e.Exception.InnerException?.Message}\n\n" +
                       $"Stack:\n{e.Exception.StackTrace}";
        
        File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash_ui.log"), errorMsg);
        MessageBox.Show(errorMsg, "Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
        Shutdown(1);
    }
    
    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var ex = e.ExceptionObject as Exception;
        var errorMsg = $"Unhandled Domain Exception:\n\n" +
                       $"Type: {ex?.GetType().Name}\n" +
                       $"Message: {ex?.Message}\n\n" +
                       $"Inner: {ex?.InnerException?.Message}\n\n" +
                       $"Stack:\n{ex?.StackTrace}";
        
        File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash_domain.log"), errorMsg);
        MessageBox.Show(errorMsg, "Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "onstartup.log"), 
                $"\n[{DateTime.Now:HH:mm:ss}] OnStartup START\n");
            
            File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "onstartup.log"), 
                $"[{DateTime.Now:HH:mm:ss}] ServiceProvider null? {ServiceProvider == null}\n");
            
            // Resolve MainWindow from DI container
            File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "onstartup.log"), 
                $"[{DateTime.Now:HH:mm:ss}] Attempting to resolve MainWindow...\n");
            
            var mainWindow = ServiceProvider?.GetRequiredService<MainWindow>();
            
            File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "onstartup.log"), 
                $"[{DateTime.Now:HH:mm:ss}] MainWindow resolved: {mainWindow != null}\n");
            
            if (mainWindow != null)
            {
                File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "onstartup.log"), 
                    $"[{DateTime.Now:HH:mm:ss}] Calling mainWindow.Show()...\n");
                
                mainWindow.Show();
                
                File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "onstartup.log"), 
                    $"[{DateTime.Now:HH:mm:ss}] OnStartup SUCCESS\n");
            }
            else
            {
                var msg = "ServiceProvider returned null!";
                File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "onstartup.log"), 
                    $"[{DateTime.Now:HH:mm:ss}] ERROR: {msg}\n");
                MessageBox.Show(msg, "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(1);
            }
        }
        catch (Exception ex)
        {
            var errorMsg = $"[{DateTime.Now:HH:mm:ss}] EXCEPTION in OnStartup:\n" +
                           $"Type: {ex.GetType().FullName}\n" +
                           $"Message: {ex.Message}\n" +
                           $"Inner: {ex.InnerException?.Message}\n" +
                           $"Stack:\n{ex.StackTrace}\n";
            
            File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash_onstartup.log"), errorMsg);
            
            MessageBox.Show(
                $"Failed to start application:\n\n" +
                $"Error: {ex.GetType().Name}\n" +
                $"Message: {ex.Message}\n\n" +
                $"Inner: {ex.InnerException?.Message}\n\n" +
                $"See crash_onstartup.log for details.",
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


