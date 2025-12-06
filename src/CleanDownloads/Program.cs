using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using CleanDownloads.Extensions;
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
        GlobalHost = CreateHostBuilder(args).Build();
        
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
            .LogToTrace();

    private static HostApplicationBuilder CreateHostBuilder(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        builder.Logging
            .ClearProviders()
            .SetMinimumLevel(LogLevel.Information)
            .AddFilter("Microsoft", LogLevel.Warning)
            .AddFilter("Microsoft.Hosting.Lifetime", LogLevel.Warning)
            .AddConsole();

        builder.Services
            .AddProcessMonitor()
            .AddFileRecycler()
            .AddInstaller();

        return builder;
    }
}