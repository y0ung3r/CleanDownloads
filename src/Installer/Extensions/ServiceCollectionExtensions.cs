using Microsoft.Extensions.DependencyInjection;

namespace Installer.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInstaller(this IServiceCollection services)
        => services.AddSingleton<ConsoleInstaller>();
}