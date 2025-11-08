using System.Collections.Concurrent;
using System.Management;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace CleanDownloads;

public sealed class DownloadsWatcher : BackgroundService
{
    private readonly ConcurrentDictionary<uint, Task> _pendingWatchings = new();
    private readonly ManagementEventWatcher _launchingWatcher = new(Wmi.Queries.ProcessLaunched);
    private readonly ManagementEventWatcher _terminationWatcher = new(Wmi.Queries.ProcessTerminated);
    
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _launchingWatcher.EventArrived += OnProcessLaunched;
        
        _launchingWatcher.Start();
        _terminationWatcher.Start();
        
        return Task.CompletedTask;
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _launchingWatcher.EventArrived -= OnProcessLaunched;
        
        _launchingWatcher.Stop();
        _launchingWatcher.Dispose();
        
        _terminationWatcher.Stop();
        _terminationWatcher.Dispose();
        
        return base.StopAsync(cancellationToken);
    }

    private Task WaitProcessTerminationFor(TrackingFile file)
    {
        while (true)
        {
            var terminationEvent = default(ManagementBaseObject?);
            
            try
            {
                terminationEvent = _terminationWatcher.WaitForNextEvent();
            }
            catch
            {
                continue;
            }
            
            var nextProcess = TrackingProcess.From(terminationEvent);

            if (nextProcess.Id != file.ProcessId) 
                continue;
            
            file.Remove();
            _pendingWatchings.TryRemove(file.ProcessId, out _);
            
            return Task.CompletedTask;
        }
    }

    private void OnProcessLaunched(object _, EventArrivedEventArgs eventArgs)
    {
        var trackingProcess = TrackingProcess.From(eventArgs.NewEvent);

        if (!trackingProcess.IsTriggeredFromDownloadsFolder())
            return;

        var trackingFile = default(TrackingFile?);
        
        try
        {
            trackingFile = trackingProcess.TrackFile();
        }
        catch
        {
            return;
        }
        
        var watchingTask = Task.Run(async () => await WaitProcessTerminationFor(trackingFile));
        _pendingWatchings.TryAdd(trackingProcess.Id, watchingTask);
    }
}