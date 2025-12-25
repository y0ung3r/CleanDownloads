using Microsoft.VisualBasic.FileIO;
using TruePath;

namespace CleanDownloads;

public sealed record TrackingFile(uint ProcessId, AbsolutePath FilePath)
{
    public void Recycle(RecycleOption recycleOptions)
    {
        var filePath = FilePath.ToString();
        FileSystem.DeleteFile(filePath, UIOption.OnlyErrorDialogs, recycleOptions);
    }
}