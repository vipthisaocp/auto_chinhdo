using System.Windows;
using AutoEHT.Models;
using AutoEHT.Services;
using AutoEHT.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace AutoEHT;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private ServiceProvider? _serviceProvider;
    
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        // Configure DI
        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();
        
        // Create and show main window
        var mainWindow = new MainWindow
        {
            DataContext = _serviceProvider.GetRequiredService<MainViewModel>()
        };
        mainWindow.Show();
    }
    
    private static void ConfigureServices(IServiceCollection services)
    {
        // Settings
        services.AddSingleton<AppSettings>();
        
        // Services
        services.AddSingleton<WindowService>();
        services.AddSingleton<ILDPlayerService, LDPlayerService>();
        services.AddSingleton<IImageMatchService, ImageMatchService>();
        services.AddSingleton<IAdbService, AdbService>();
        services.AddSingleton<GameScriptRunner>();
        services.AddSingleton<RecordService>();
        
        // ViewModels
        services.AddTransient<MainViewModel>();
    }
    
    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }
}
