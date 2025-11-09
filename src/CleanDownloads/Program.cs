using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Logging.EventLog;
using CleanDownloads;

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
    .AddHostedService<DownloadsWatcher>()
    .AddWindowsService(options => options.ServiceName = "Clean Downloads");

LoggerProviderOptions.RegisterProviderOptions<EventLogSettings, EventLogLoggerProvider>(builder.Services);

var host = builder.Build();

await host.RunAsync();