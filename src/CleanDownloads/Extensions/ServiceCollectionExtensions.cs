using CleanDownloads.Processes;
using Microsoft.Extensions.DependencyInjection;

namespace CleanDownloads.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddProcessMonitor(this IServiceCollection services)
        => services.AddSingleton<ProcessMonitor>();
    
    public static IServiceCollection AddFileRecycler(this IServiceCollection services)
        => services.AddHostedService<FileRecycler>();
}