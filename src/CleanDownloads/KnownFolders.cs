using System;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;
using Microsoft.Win32.SafeHandles;
using TruePath;

namespace CleanDownloads;

public static class KnownFolders
{
    public static class Downloads
    {
        private static readonly Guid DownloadsFolderVariable
            = new("374DE290-123F-4565-9164-39C4925E467B");
        
        public static AbsolutePath Path
        {
            get
            {
                var searchResult = PInvoke.SHGetKnownFolderPath(
                    DownloadsFolderVariable, 
                    KNOWN_FOLDER_FLAG.KF_FLAG_DEFAULT, 
                    hToken: new SafeAccessTokenHandle(HANDLE.Null),
                    out var folderPath);

                searchResult.ThrowOnFailure();

                try
                {
                    return new AbsolutePath(folderPath.ToString());
                }
                finally
                {
                    unsafe
                    {
                        PInvoke.CoTaskMemFree(folderPath);
                    }
                }
            }
        }
    }
}