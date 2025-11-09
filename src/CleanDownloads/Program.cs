using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using CleanDownloads;
using CleanDownloads.Processes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.EventLog;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging
    .ClearProviders()
    .SetMinimumLevel(LogLevel.Information)
    .AddFilter<EventLogLoggerProvider>("Microsoft.Hosting.Lifetime", LogLevel.Warning)
    .AddFilter<EventLogLoggerProvider>(string.Empty, LogLevel.Information)
    .AddConsole()
    .AddEventLog(new EventLogSettings
    {
        SourceName = "Clean Downloads",
        LogName = "Application",
        Filter = (_, logLevel) => logLevel >= LogLevel.Information
    });

builder.Services
    .AddSingleton<ProcessMonitor>()
    .AddHostedService<WindowsService>()
    .AddWindowsService(options => options.ServiceName = "Clean Downloads");

var host = builder.Build();

await host.RunAsync();