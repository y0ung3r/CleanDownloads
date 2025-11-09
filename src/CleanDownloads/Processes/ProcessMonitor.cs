using System;
using System.Management;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace CleanDownloads.Processes;

public sealed class ProcessMonitor : IDisposable
{
    private readonly ILogger<ProcessMonitor> _logger;
    private readonly ManagementEventWatcher _launchingWatcher = new(WindowsManagementInstrumentation.Processes.SelectLaunched);
    private readonly ManagementEventWatcher _terminatingWatcher = new(WindowsManagementInstrumentation.Processes.SelectTerminated);

    public ProcessMonitor(ILogger<ProcessMonitor> logger)
    {
        _logger = logger;
        
        _terminatingWatcher.Start();
        _launchingWatcher.Start();
        
        _logger.LogInformation("[CleanDownloads]: Process monitor has been started");
    }

    public Task<TrackingProcess?> WaitForNextLaunchingProcessAsync(CancellationToken cancellationToken)
        => WaitForNextProcessAsync(_launchingWatcher, cancellationToken);
    
    public Task<TrackingProcess?> WaitForNextTerminatingProcessAsync(CancellationToken cancellationToken)
        => WaitForNextProcessAsync(_terminatingWatcher, cancellationToken);

    private static async Task<TrackingProcess?> WaitForNextProcessAsync(ManagementEventWatcher processWatcher, CancellationToken cancellationToken)
    {
        try
        {
            var nextProcess = await Task.Run(processWatcher.WaitForNextEvent, cancellationToken);
            return TrackingProcess.From(nextProcess);
        }
        catch
        {
            return null;
        }
    }

    public void Dispose()
    {
        _launchingWatcher.Stop();
        _launchingWatcher.Dispose();
        
        _terminatingWatcher.Stop();
        _terminatingWatcher.Dispose();
        
        _logger.LogInformation("[CleanDownloads]: Process monitor has been stopped");
    }
}