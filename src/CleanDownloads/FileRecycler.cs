using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CleanDownloads.Processes;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CleanDownloads;

public sealed class FileRecycler(ILogger<FileRecycler> logger, ProcessMonitor processMonitor) : BackgroundService
{
    private readonly ConcurrentDictionary<uint, TrackingProcess?> _missingProcesses = new();
    private readonly ConcurrentDictionary<uint, Task> _pendingRecycles = new();
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("[CleanDownloads]: Background service has been started");
        
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var trackingProcess = await processMonitor.WaitForNextLaunchingProcessAsync(stoppingToken);
                
                if (trackingProcess is null)
                    continue;
                
                logger.LogInformation(
                    "[CleanDownloads]: Received process {ProcessName} ({ProcessId}), which may need to be tracked", 
                    trackingProcess.Name, 
                    trackingProcess.Id);

                if (!trackingProcess.IsTriggeredFromDownloadsFolder())
                {
                    logger.LogInformation(
                        "[CleanDownloads]: The process {ProcessName} ({ProcessId}) was not launched from the Downloads folder, so tracking will not be performed", 
                        trackingProcess.Name, 
                        trackingProcess.Id);
                    
                    continue;
                }

                if (!trackingProcess.TryTrackFile(out var trackingFile))
                {
                    logger.LogWarning(
                        "[CleanDownloads]: The process {ProcessName} ({ProcessId}) cannot be tracked because the file path could not be obtained", 
                        trackingProcess.Name, 
                        trackingProcess.Id);
                    
                    continue;
                }

                ScheduleRecycleFor(trackingProcess, trackingFile, stoppingToken);
                
                logger.LogInformation("[CleanDownloads]: The file \"{FilePath}\" is scheduled for deletion", trackingFile.FilePath);
            }
        }
        catch (OperationCanceledException)
        {
            // Ignore
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "[CleanDownloads]: An unexpected error has occurred");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("[CleanDownloads]: Background service is stopping");
        
        await Task.WhenAll(_pendingRecycles.Values);
        
        await base.StopAsync(cancellationToken);
        
        logger.LogInformation("[CleanDownloads]: Background service has been stopped");
    }
    
    private async Task RecycleForAsync(TrackingFile file, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var terminatedProcess = _missingProcesses.GetValueOrDefault(file.ProcessId, await processMonitor.WaitForNextTerminatingProcessAsync(cancellationToken));

            if (terminatedProcess is null)
                continue;

            if (terminatedProcess.Id != file.ProcessId)
            {
                _missingProcesses.TryAdd(terminatedProcess.Id, terminatedProcess);

                continue;
            }

            file.Remove(); // TODO: Delete permanently
            
            FinishRecycle(file);
            
            logger.LogInformation("[CleanDownloads]: The file {FileName} has been successfully deleted", file.FilePath);
        }
    }
    
    private void ScheduleRecycleFor(TrackingProcess process, TrackingFile file, CancellationToken cancellationToken)
    {
        var recyclingTask = Task.Run(async () =>
        {
            try
            {
                await RecycleForAsync(file, cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "[CleanDownloads]: Unable to recycle file {FileName}", file.FilePath);
                
                FinishRecycle(file);
            }
        }, 
        cancellationToken);
        
        _pendingRecycles.TryAdd(process.Id, recyclingTask);
    }
    
    private void FinishRecycle(TrackingFile file)
    {
        _pendingRecycles.TryRemove(file.ProcessId, out _);
        _missingProcesses.TryRemove(file.ProcessId, out _);
    }
}