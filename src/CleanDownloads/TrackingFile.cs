using System;
using Microsoft.VisualBasic.FileIO;
using TruePath;

namespace CleanDownloads;

public sealed record TrackingFile(uint ProcessId, AbsolutePath FilePath)
{
    public void Recycle(RecycleMode recycleMode)
    {
        if (recycleMode is RecycleMode.Disable)
            return;
        
        var filePath = FilePath.ToString();
        var recycleOption = recycleMode switch
        {
            RecycleMode.DeletePermanently => RecycleOption.DeletePermanently,
            RecycleMode.SendToRecycleBin => RecycleOption.SendToRecycleBin,
            _ => throw new InvalidOperationException("Invalid recycle mode")
        };
        
        FileSystem.DeleteFile(filePath, UIOption.OnlyErrorDialogs, recycleOption);
    }
}