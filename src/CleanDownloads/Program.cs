using Microsoft.Extensions.Hosting;
using CleanDownloads.Extensions;
using Installer.Extensions;
using Microsoft.Extensions.Logging;

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

var host = builder.Build();

host.UseInstaller();

await host.RunAsync();