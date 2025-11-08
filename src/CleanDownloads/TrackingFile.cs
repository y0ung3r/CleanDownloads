using Microsoft.VisualBasic.FileIO;

namespace CleanDownloads;

public sealed record TrackingFile(uint ProcessId, string FilePath)
{
    public void Remove() 
        => FileSystem.DeleteFile(FilePath, UIOption.OnlyErrorDialogs,  RecycleOption.SendToRecycleBin);
    
    public void Delete()
        => FileSystem.DeleteFile(FilePath, UIOption.OnlyErrorDialogs, RecycleOption.DeletePermanently);
}