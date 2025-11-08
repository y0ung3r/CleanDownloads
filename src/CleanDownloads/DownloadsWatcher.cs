using System.Collections.Concurrent;
using System.Management;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CleanDownloads;

public sealed class DownloadsWatcher(ILogger<DownloadsWatcher> logger) : BackgroundService
{
    private readonly ConcurrentDictionary<uint, Task> _pendingWatchings = new();
    private readonly ManagementEventWatcher _launchingWatcher = new(Wmi.Queries.ProcessLaunched);
    private readonly ManagementEventWatcher _terminationWatcher = new(Wmi.Queries.ProcessTerminated);
    
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _launchingWatcher.EventArrived += OnProcessLaunched;
        
        _launchingWatcher.Start();
        _terminationWatcher.Start();
        
        logger.LogInformation("Watcher for Downloads folder is started");
        
        return Task.CompletedTask;
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _launchingWatcher.EventArrived -= OnProcessLaunched;
        
        _launchingWatcher.Stop();
        _launchingWatcher.Dispose();
        
        _terminationWatcher.Stop();
        _terminationWatcher.Dispose();
        
        logger.LogInformation("Watcher for Downloads folder is stopped");
        
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
            
            logger.LogInformation("File {FilePath} already removed", file.FilePath);
            
            return Task.CompletedTask;
        }
    }

    private void OnProcessLaunched(object _, EventArrivedEventArgs eventArgs)
    {
        var trackingProcess = TrackingProcess.From(eventArgs.NewEvent);

        logger.LogInformation("An audit has been initiated for process {ProcessId}: {CommandLine}", trackingProcess.Id, trackingProcess.CommandLine);

        if (!trackingProcess.IsTriggeredFromDownloadsFolder())
        {
            logger.LogInformation("Process {ProcessId} skipped", trackingProcess.Id);
            
            return;
        }

        var trackingFile = default(TrackingFile?);
        
        try
        {
            trackingFile = trackingProcess.TrackFile();
        }
        catch
        {
            return;
        }
        
        logger.LogInformation("Prepare file {FilePath} which triggered process {ProcessId}", trackingFile.FilePath, trackingFile.ProcessId);
        
        var watchingTask = Task.Run(async () => await WaitProcessTerminationFor(trackingFile));
        _pendingWatchings.TryAdd(trackingProcess.Id, watchingTask);
    }
}