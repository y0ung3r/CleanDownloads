using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.ReactiveUI;
using Avalonia.Threading;
using CleanDownloads.Extensions;
using CleanDownloads.ViewModels;
using Installer.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CleanDownloads;

public static class Program
{
    private static IHost? _globalHost;

    public static IHost GlobalHost
    {
        get => _globalHost ?? throw new InvalidOperationException("Global host is unavailable");
        private set => _globalHost = value;
    }
    
    [STAThread]
    public static async Task Main(string[] args)
    {
        var hostBuilder = await CreateHostBuilder(args);
        
        GlobalHost = hostBuilder.Build();
        
        GlobalHost.UseInstaller();
        
        var hostLifetime = GlobalHost.Services.GetRequiredService<IHostApplicationLifetime>();
        
        hostLifetime.ApplicationStarted.Register(() =>
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        });
        
        hostLifetime.ApplicationStopping.Register(() =>
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime)
                Dispatcher.UIThread.InvokeShutdown();
        });

        await GlobalHost.RunAsync();
    }

    private static AppBuilder BuildAvaloniaApp()
        => AppBuilder
            .Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
            .UseReactiveUI();

    private static async Task<HostApplicationBuilder> CreateHostBuilder(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        var logging = builder.Logging;
        var services = builder.Services;

        logging
            .ClearProviders()
            .SetMinimumLevel(LogLevel.Information)
            .AddFilter("Microsoft", LogLevel.Warning)
            .AddFilter("Microsoft.Hosting.Lifetime", LogLevel.Warning)
            .AddConsole();

        services
            .AddProcessMonitor()
            .AddFileRecycler()
            .AddInstaller()
            .AddTransient<MainWindowViewModel>()
            .AddSingleton(await CleaningSettings.LoadAsync(CleaningSettings.DefaultPath));

        return builder;
    }
}