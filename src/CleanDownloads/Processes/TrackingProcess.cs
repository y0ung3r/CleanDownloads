using System;
using System.IO;
using System.Linq;
using System.Management;
using Windows.Win32;
using Windows.Win32.Foundation;
using TruePath;

namespace CleanDownloads.Processes;

public sealed class TrackingProcess
{
    public static TrackingProcess From(ManagementBaseObject process)
    {
        var instance = (ManagementBaseObject)process[WindowsManagementInstrumentation.Processes.TargetInstance];
        var processId = Convert.ToUInt32(instance.Properties[WindowsManagementInstrumentation.Processes.ProcessId].Value);
        var processName = Convert.ToString(instance.Properties[WindowsManagementInstrumentation.Processes.ProcessName].Value);
        var commandLine = Convert.ToString(instance.Properties[WindowsManagementInstrumentation.Processes.CommandLine].Value);

        return new TrackingProcess(processId, processName, commandLine);
    }

    public uint Id { get; }
    
    public string? Name { get; }
    
    public string? CommandLine { get; }
    
    private AbsolutePath? FilePath { get; }

    private TrackingProcess(uint id, string? name, string? commandLine)
    {
        Id = id;
        Name = name;
        CommandLine = commandLine;
        FilePath = ExtractFilePath(SplitArgs(CommandLine));
    }

    public bool TryTrackFile(out TrackingFile trackingFile)
    {
        trackingFile = null!;
        
        if (FilePath is null)
            return false;
        
        trackingFile = new TrackingFile(Id, FilePath.Value);
        
        return true;
    }

    public bool IsTriggeredFromDownloadsFolder()
    {
        if (FilePath is null)
            return false;
        
        return KnownFolders.Downloads.Path.IsPrefixOf(FilePath.Value);
    }

    private static AbsolutePath? ExtractFilePath(string[] arguments)
    {
        if (arguments.Length is 0) 
            return null;
        
        foreach (var argument in arguments.Skip(count: 1))
        {
            var extension = Path.GetExtension(argument);
            
            if (!string.IsNullOrWhiteSpace(extension) && File.Exists(argument))
                return new AbsolutePath(argument);
        }
        
        return null;
    }
    
    private static unsafe string[] SplitArgs(string? commandLine)
    {
        if (string.IsNullOrWhiteSpace(commandLine)) 
            return [];

        var pointer = PInvoke.CommandLineToArgv(commandLine, out var argc);

        if (pointer is null || argc is 0)
            return [commandLine];

        try
        {
            var span = new ReadOnlySpan<PWSTR>(pointer, argc);
            var argv = new string[argc];

            for (var index = 0; index < argc; index++)
                argv[index] = span[index].ToString();

            return argv;
        }
        finally
        {
            var local = new HLOCAL(pointer);
            var handle = PInvoke.LocalFree_SafeHandle(local);
            handle.Dispose();
        }
    }
}