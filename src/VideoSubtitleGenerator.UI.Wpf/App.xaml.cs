using System.Configuration;
using System.Data;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
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


