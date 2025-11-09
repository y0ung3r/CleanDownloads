using Microsoft.VisualBasic.FileIO;

namespace CleanDownloads;

public sealed record TrackingFile(uint ProcessId, string FilePath)
{
    public void Remove() 
        => Recycle(RecycleOption.SendToRecycleBin); // TODO: Deletion from LocalSystem profile always permanently 
    
    public void Delete()
        => Recycle(RecycleOption.DeletePermanently);
    
    private void Recycle(RecycleOption recycleOptions) 
        => FileSystem.DeleteFile(FilePath, UIOption.OnlyErrorDialogs, recycleOptions);
}