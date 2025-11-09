using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using CleanDownloads;
using CleanDownloads.Processes;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging
    .ClearProviders()
    .AddConsole();

builder.Services
    .AddSingleton<ProcessMonitor>()
    .AddHostedService<FileRecycler>();

var host = builder.Build();

await host.RunAsync();