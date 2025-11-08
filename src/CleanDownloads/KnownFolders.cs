using System;
using System.Runtime.InteropServices;

namespace CleanDownloads;

public static class KnownFolders
{
    private static readonly Guid DownloadsIdentifier = new("374DE290-123F-4565-9164-39C4925E467B");
    
    public static string Downloads
    {
        get
        {
            var searchResult = SHGetKnownFolderPath(DownloadsIdentifier, dwFlags: 0, hToken: IntPtr.Zero, out var folderPath);
            
            if (searchResult is not 0) 
                Marshal.ThrowExceptionForHR(searchResult);

            try
            {
                return Marshal.PtrToStringUni(folderPath) 
                    ?? throw new InvalidOperationException("Unable to determine download folder path");
            }
            finally
            {
                if (folderPath != IntPtr.Zero)
                    Marshal.FreeCoTaskMem(folderPath);
            }
        }
    }
    
    [DllImport("shell32.dll")]
    private static extern int SHGetKnownFolderPath([MarshalAs(UnmanagedType.LPStruct)] Guid rfid, uint dwFlags, IntPtr hToken, out IntPtr ppszPath);
}