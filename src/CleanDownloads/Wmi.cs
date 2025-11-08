namespace CleanDownloads;

public static class Wmi
{
    public static class Processes
    {
        public const string TargetInstance = nameof(TargetInstance);
        public const string ProcessId = nameof(ProcessId);
        public const string CommandLine = nameof(CommandLine);
    }
    
    public static class Queries
    {
        public const string ProcessLaunched = "select * from __InstanceCreationEvent within 1 where TargetInstance isa 'Win32_Process'";
        public const string ProcessTerminated = "select * from __InstanceDeletionEvent within 1 where TargetInstance isa 'Win32_Process'";
    }
}