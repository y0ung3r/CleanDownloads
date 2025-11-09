using Microsoft.VisualBasic.FileIO;
using TruePath;

namespace CleanDownloads;

public sealed record TrackingFile(uint ProcessId, AbsolutePath FilePath)
{
    public void Remove() 
        => Recycle(RecycleOption.SendToRecycleBin); // TODO: Deletion from LocalSystem profile always permanently 
    
    public void Delete()
        => Recycle(RecycleOption.DeletePermanently);
    
    private void Recycle(RecycleOption recycleOptions) 
        => FileSystem.DeleteFile(FilePath.ToString(), UIOption.OnlyErrorDialogs, recycleOptions);
}