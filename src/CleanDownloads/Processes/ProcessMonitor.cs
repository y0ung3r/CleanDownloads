using System;
using System.Management;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace CleanDownloads.Processes;

public sealed class ProcessMonitor(ILogger<ProcessMonitor> logger) : IDisposable
{
    private readonly ManagementEventWatcher _launchingWatcher = new(WindowsManagementInstrumentation.Processes.SelectLaunched);
    private readonly ManagementEventWatcher _terminatingWatcher = new(WindowsManagementInstrumentation.Processes.SelectTerminated);

    public Task StartAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        _terminatingWatcher.Start();
        _launchingWatcher.Start();
        
        logger.LogInformation("Process monitor has been started");
        
        return Task.CompletedTask;
    }

    public Task<TrackingProcess?> WaitForNextLaunchingProcessAsync(CancellationToken cancellationToken)
        => WaitForNextProcessAsync(_launchingWatcher, cancellationToken);
    
    public Task<TrackingProcess?> WaitForNextTerminatingProcessAsync(CancellationToken cancellationToken)
        => WaitForNextProcessAsync(_terminatingWatcher, cancellationToken);

    private static async Task<TrackingProcess?> WaitForNextProcessAsync(ManagementEventWatcher processWatcher, CancellationToken cancellationToken)
    {
        var completionSource = new TaskCompletionSource<ManagementBaseObject>();
        await using var registration = cancellationToken.Register(() => completionSource.TrySetCanceled());

        var eventHandler = new EventArrivedEventHandler((_, eventArgs) => completionSource.TrySetResult(eventArgs.NewEvent));

        try
        {
            processWatcher.EventArrived += eventHandler;

            return TrackingProcess.From(await completionSource.Task);
        }
        catch
        {
            return null;
        }
        finally
        {
            processWatcher.EventArrived -= eventHandler;
        }
    }

    public void Dispose()
    {
        _launchingWatcher.Stop();
        _launchingWatcher.Dispose();
        
        _terminatingWatcher.Stop();
        _terminatingWatcher.Dispose();
        
        logger.LogInformation("Process monitor has been stopped");
    }
}