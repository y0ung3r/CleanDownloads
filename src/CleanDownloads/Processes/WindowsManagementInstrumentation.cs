namespace CleanDownloads.Processes;

public static class WindowsManagementInstrumentation
{
    public static class Processes
    {
        public const string TargetInstance = nameof(TargetInstance);
        
        public const string ProcessId = nameof(ProcessId);
        
        public const string ProcessName = "Name";
        
        public const string CommandLine = nameof(CommandLine);
        
        public const string SelectLaunched = "select * from __InstanceCreationEvent within 1 where TargetInstance isa 'Win32_Process'";
        
        public const string SelectTerminated = "select * from __InstanceDeletionEvent within 1 where TargetInstance isa 'Win32_Process'";
    }
}