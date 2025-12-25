using System;
using System.IO;
using System.Threading;
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
using Nito.AsyncEx.Interop;
using Serilog;
using Serilog.Events;

namespace CleanDownloads;

public static class Program
{
    public static IHost GlobalHost
    {
        get => field ?? throw new InvalidOperationException("Global host is unavailable");
        private set;
    }

    private static ILogger<IEntryPoint> Logger
    {
        get => field ?? throw new InvalidOperationException("Logger is unavailable");
        set;
    }

    [STAThread]
    public static async Task Main(string[] args)
    {
        var hostBuilder = await CreateHostBuilder(args);
        
        GlobalHost = hostBuilder.Build();
        
        Logger = GlobalHost
            .Services
            .GetRequiredService<ILogger<IEntryPoint>>();
        
        GlobalHost.UseInstaller();

#if !DEBUG
        using var handle = new EventWaitHandle(initialState: true, EventResetMode.AutoReset, nameof(CleanDownloads), out var isAcquired);

        if (!isAcquired)
        {
            Logger.LogInformation("Found already running process. Sending awakening signal...");
            
            handle.Set();
            
            Environment.Exit(exitCode: 0);
        }
#else
        Logger.LogInformation("Host must be started immediately, because we are in DEBUG mode");
#endif
        
        var hostLifetime = GlobalHost.Services.GetRequiredService<IHostApplicationLifetime>();
        
        hostLifetime.ApplicationStarted.Register(() =>
        {
            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        });
        
        hostLifetime.ApplicationStopping.Register(() =>
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime)
                Dispatcher.UIThread.InvokeShutdown();
        });
        
#if !DEBUG
        WaitAwakeningSignal(handle);
#endif
        
        await GlobalHost.RunAsync();

#if !DEBUG
        handle.Reset();
#endif
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

        var logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override(source: "Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override(source: "Microsoft.Hosting.Lifetime", LogEventLevel.Warning)
            .Enrich.WithProperty(name: "SessionId", value: Guid.NewGuid().ToString("N")[..8])
            .WriteTo.Console(outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{SessionId}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                path: Path.Combine(AppContext.BaseDirectory, "CleanDownloads.log"), 
                shared: true, 
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{SessionId}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();
        
        logging
            .ClearProviders()
            .AddSerilog(logger, dispose: true);

        services
            .AddProcessMonitor()
            .AddFileRecycler()
            .AddInstaller()
            .AddTransient<MainWindowViewModel>()
            .AddSingleton(await CleaningSettings.LoadAsync(CleaningSettings.DefaultPath));

        return builder;
    }
    
    private static void WaitAwakeningSignal(EventWaitHandle handle)
    {
        handle.WaitOne();
        
        _ = Task.Run(async () =>
        {
            while (true)
            {
                Logger.LogInformation("Waiting for awakening signal to show existing window...");
                
                await WaitHandleAsyncFactory.FromWaitHandle(handle);
                
                Logger.LogInformation("Awakening signal is received. Trying to show existing window...");
                
                Dispatcher.UIThread.Post(() =>
                {
                    var lifetime = (IClassicDesktopStyleApplicationLifetime?)Application.Current?.ApplicationLifetime;
                    
                    lifetime?
                        .MainWindow?
                        .Show();
                });
                
                Logger.LogInformation("Window showing is already scheduled");
            }
            
            // ReSharper disable once FunctionNeverReturns
        });
    }
}