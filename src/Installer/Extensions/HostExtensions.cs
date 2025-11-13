using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Installer.Extensions;

public static class HostExtensions
{
    public static void UseInstaller(this IHost host)
        => host
            .Services
            .GetRequiredService<ConsoleInstaller>()
            .Install();
}