using System;
using System.IO;
using System.Linq;
using Microsoft.Win32;

namespace CleanDownloads;

public static class KnownFolders
{
    private static readonly string[] SystemProfiles =
    [
        // Default
        ".DEFAULT", 
        
        // Local system
        "S-1-5-18", 
        
        // Local service
        "S-1-5-19", 
        
        // Network service
        "S-1-5-20"
    ];
    
    private const string ProfileListRegistryKey
        = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\ProfileList";
    
    private const string ProfileImagePathVariable
        = "ProfileImagePath";
    
    private const string UserShellFoldersRegistryKeyTemplate
        = @"{0}\Software\Microsoft\Windows\CurrentVersion\Explorer\User Shell Folders";
    
    private const string UserProfileVariable
        = "%USERPROFILE%";
    
    private static string ResolveUserProfilePath(RegistryKey profile)
    {
        var profileImagePathVariable = profile.GetValue(ProfileImagePathVariable);
        
        if (profileImagePathVariable is not string profileFolderPath)
            throw new InvalidOperationException("Unable to find user profile path");
        
        if (!Directory.Exists(profileFolderPath))
            throw new InvalidOperationException("User profile path is not exists");

        return Environment.ExpandEnvironmentVariables(profileFolderPath); // Expand variables such as %SystemDrive%
    }

    public static class Downloads
    {
        private const string DownloadsFolderVariable
            = "{374DE290-123F-4565-9164-39C4925E467B}";
        
        public static string Path
        {
            get
            {
                using var profileList = Registry.LocalMachine.OpenSubKey(ProfileListRegistryKey) 
                    ?? throw new InvalidOperationException("Unable to find Profile List registry key");
                
                var profileName = profileList
                    .GetSubKeyNames()
                    .FirstOrDefault(registryKey => !SystemProfiles.Contains(registryKey));
                
                if (profileName is null)
                    throw new InvalidOperationException("Unable to find user profile");
                
                using var latestUser = profileList.OpenSubKey(profileName) 
                    ?? throw new InvalidOperationException("Unable to find user profile");
                
                var profilePath = ResolveUserProfilePath(latestUser);
                
                using var userShellFolders = Registry.Users.OpenSubKey(string.Format(UserShellFoldersRegistryKeyTemplate, profileName))
                    ?? throw new InvalidOperationException("Unable to find User Shell Folders for current user");

                var downloadsFolderVariable = userShellFolders.GetValue(DownloadsFolderVariable, defaultValue: null, RegistryValueOptions.DoNotExpandEnvironmentNames);
                
                if (downloadsFolderVariable is not string downloadsFolderPath)
                    throw new InvalidOperationException("Unable to find Downloads folder variable for current user");
                
                var expandedDownloadsPath = Environment.ExpandEnvironmentVariables(
                    downloadsFolderPath.Replace(UserProfileVariable, profilePath, StringComparison.OrdinalIgnoreCase)); // Expand other variables if needed

                if (!Directory.Exists(expandedDownloadsPath))
                    throw new InvalidOperationException($"Downloads path \"{expandedDownloadsPath}\" is not exists");
                
                return expandedDownloadsPath;
            }
        }
    }
}