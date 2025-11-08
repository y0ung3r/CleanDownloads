using System;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;

namespace CleanDownloads;

public sealed record TrackingProcess
{
    public static TrackingProcess From(ManagementBaseObject process)
    {
        var instance = (ManagementBaseObject)process[Wmi.Processes.TargetInstance];
        var processId = Convert.ToUInt32(instance.Properties[Wmi.Processes.ProcessId].Value);
        var commandLine = Convert.ToString(instance.Properties[Wmi.Processes.CommandLine].Value);
        // TODO: Consider to use var executablePath = Convert.ToString(processInstance.Properties["ExecutablePath"].Value);

        return new TrackingProcess(processId, commandLine);
    }

    public uint Id { get; }
    
    public string? CommandLine { get; }
    
    private string? FilePath { get; }

    private TrackingProcess(uint id, string? commandLine)
    {
        Id = id;
        CommandLine = commandLine;
        FilePath = ExtractFilePath(SplitArgs(CommandLine));
    }

    public TrackingFile TrackFile()
    {
        if (string.IsNullOrWhiteSpace(FilePath))
            throw new InvalidOperationException("Unable to determine file path");
        
        return new TrackingFile(Id, FilePath);
    }

    public bool IsTriggeredFromDownloadsFolder()
        => FilePath?.Contains(KnownFolders.Downloads, StringComparison.OrdinalIgnoreCase) is true;

    private static string? ExtractFilePath(string[] arguments)
    {
        if (arguments.Length is 0) 
            return null;
        
        foreach (var argument in arguments.Skip(count: 1))
        {
            var extension = Path.GetExtension(argument);
            
            if (!string.IsNullOrWhiteSpace(extension) && File.Exists(argument))
                return argument;
        }
        
        return null;
    }
    
    private static string[] SplitArgs(string? commandLine)
    {
        if (string.IsNullOrWhiteSpace(commandLine)) 
            return [];
        
        var pointer = CommandLineToArgvW(commandLine, out var argc);
        
        if (pointer == IntPtr.Zero) 
            return [commandLine];
        
        try
        {
            var argv = new string[argc];
            
            for (var index = 0; index < argc; index++) 
                argv[index] = Marshal.PtrToStringUni(Marshal.ReadIntPtr(pointer, IntPtr.Size * index)) 
                    ?? throw new InvalidOperationException("Unable to determine command line argument");
            
            return argv;
        }
        finally
        {
            LocalFree(pointer);
        }
    }
    
    [DllImport("shell32.dll", SetLastError = true)]
    private static extern IntPtr CommandLineToArgvW([MarshalAs(UnmanagedType.LPWStr)] string lpCmdLine, out int pNumArgs);

    [DllImport("kernel32.dll")]
    private static extern IntPtr LocalFree(IntPtr hMem);
}