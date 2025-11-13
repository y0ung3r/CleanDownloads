using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.Extensions.Logging;
using TruePath;
using TruePath.SystemIo;

namespace Installer;

/*
 * The following code was used as a reference: https://github.com/SKProCH/DiscordScreenshareLoopbackShutup/blob/master/DiscordScreenshareLoopbackShutup/Services/InstallerService.cs
 * Thank you, SKProCH!
 */

public sealed class ConsoleInstaller(ILogger<ConsoleInstaller> logger)
{
    private const string UserFriendlyApplicationName = "Clean Downloads";
    private const string ApplicationName = "CleanDownloads";

    public void Install()
    {
#if DEBUG
        return;
#endif
        
        var installerPath = GetCurrentInstallerPath();
        
        ThrowIfCompiledAsNotSingleFile(installerPath);

        var installationPath = GetInstallationPath();

        if (installerPath.Parent == installationPath.Parent)
            return;
        
        KillRunningApplicationInstances(installationPath);
        InstallExecutable(installerPath, installationPath);
        CreateStartMenuShortcut(installationPath);
        CreateScheduledTask(installationPath);
        OpenApplication(installationPath);
        
        logger.LogInformation("Installation of {ApplicationName} completed successfully", UserFriendlyApplicationName);
        
        Environment.Exit(exitCode: 0);
    }

    private static void OpenApplication(AbsolutePath installationPath) 
        => Process.Start(new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = CreateOpeningScript(installationPath)
        });

    private void CreateStartMenuShortcut(AbsolutePath installationPath)
    {
        try
        {
            var shortcutPath = GetProgramsFolderPath() / $"{UserFriendlyApplicationName}.lnk";

            if (shortcutPath.Exists())
            {
                logger.LogInformation("Shortcut for {ApplicationName} already exists", UserFriendlyApplicationName);
                
                return;
            }

            var creationScript = CreateShortcutCreationScript(shortcutPath, installationPath)
                .Replace("\"", "`\"");
            
            using var powershell = Process.Start(new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{creationScript}\"",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            });

            if (powershell is null)
                throw new InvalidOperationException($"Failed to execute Start Menu shortcut script for {UserFriendlyApplicationName}");
            
            powershell.WaitForExit();

            if (powershell.ExitCode is not 0)
                throw new InvalidOperationException($"Failed to create Start Menu shortcut for {UserFriendlyApplicationName}. Exit code: {powershell.ExitCode}");
            
            logger.LogInformation("Shortcut \"{ApplicationName}\" for {ShortcutPath} successfully created", UserFriendlyApplicationName, shortcutPath);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to create Start Menu shortcut for {ApplicationName}", UserFriendlyApplicationName);

            throw;
        }
    }
    
    private void CreateScheduledTask(AbsolutePath installationPath)
    {
        if (IsScheduledTaskExists())
        {
            logger.LogInformation("Scheduled task for {ApplicationName} already exists", UserFriendlyApplicationName);
            
            return;
        }

        var temporaryXmlPath = Temporary.CreateTempFile();
            
        try
        {
            temporaryXmlPath.WriteAllText(CreateScheduledTaskCreationScript(installationPath));
                
            using var schtasks = Process.Start(new ProcessStartInfo
            {
                FileName = "schtasks.exe",
                Arguments = $"/Create /TN \"{UserFriendlyApplicationName}\" /XML \"{temporaryXmlPath}\" /F",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            });
                
            if (schtasks is null)
                throw new InvalidOperationException("Failed to execute Scheduled Task script for {UserFriendlyApplicationName}");
                
            schtasks.WaitForExit();

            if (schtasks.ExitCode is not 0)
            {
                var error = schtasks.StandardError.ReadToEnd();
                throw new InvalidOperationException($"Failed to create scheduled task. Exit code: {schtasks.ExitCode}. Error: {error}");
            }
            
            logger.LogInformation("Scheduled task for {ApplicationName} successfully created", UserFriendlyApplicationName);
        }
        finally
        {
            temporaryXmlPath.Delete();
        }
    }

    private bool IsScheduledTaskExists()
    {
        using var schtasks = Process.Start(new ProcessStartInfo
        {
            FileName = "schtasks.exe",
            Arguments = $"/Query /TN \"{UserFriendlyApplicationName}\"",
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        });
        
        if (schtasks is null)
            throw new InvalidOperationException($"Failed to verify that Scheduled Task for {UserFriendlyApplicationName} is already exists");
        
        schtasks.WaitForExit();

        if (schtasks.ExitCode is 0) 
            return true;
        
        logger.LogInformation("Scheduled task for {ApplicationName} is not exists", UserFriendlyApplicationName);
            
        return false;
    }
    
    private void InstallExecutable(AbsolutePath installerPath, AbsolutePath installationPath)
    {
        if (installationPath.Parent is null)
            throw new InvalidOperationException("Installation path is not available");
        
        try
        {
            installationPath.Parent.Value.CreateDirectory();
            installerPath.Copy(installationPath, overwrite: true);
            
            logger.LogInformation("Executable for {ApplicationName} successfully copied to {InstallationFolder}", UserFriendlyApplicationName, installationPath.Parent);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to copy executable of {ApplicationName} to \"{InstallationFolder}\"", UserFriendlyApplicationName, installationPath.Parent);
            
            throw;
        }
    }

    private void KillRunningApplicationInstances(AbsolutePath installedExecutablePath)
    {
        var currentProcess = Process.GetCurrentProcess();
        var existingProcesses = Process
            .GetProcessesByName(currentProcess.ProcessName)
            .Where(process =>
            {
                if (string.IsNullOrWhiteSpace(process.MainModule?.FileName))
                    return false;

                if (process.Id == currentProcess.Id)
                    return false;
                
                return installedExecutablePath == new AbsolutePath(process.MainModule.FileName);
            });

        foreach (var process in existingProcesses)
        {
            try
            {
                process.Kill();
                process.WaitForExit(milliseconds: 5000);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to kill process {ProcessId}", process.Id);

                throw;
            }
        }
        
        logger.LogInformation("All existing {ApplicationName} application instances killed successfully", UserFriendlyApplicationName);
    }

    private static void ThrowIfCompiledAsNotSingleFile(AbsolutePath exePath)
    {
        if (exePath.Parent is null)
            throw new InvalidOperationException("Dll path is not available");
        
        var dllPath = exePath.Parent.Value / $"{ApplicationName}.dll";

        if (dllPath.Exists())
            throw new InvalidOperationException("Use 'dotnet publish' to get a single file or build project in 'Development' environment to debug");
    }

    private static AbsolutePath GetProgramsFolderPath()
        => new(Environment.GetFolderPath(Environment.SpecialFolder.Programs));
    
    private static AbsolutePath GetInstallationPath()
    {
        var folderPath = new AbsolutePath(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)) / ApplicationName;
        
        return folderPath / $"{ApplicationName}.exe";
    }

    private static AbsolutePath GetCurrentInstallerPath()
    {
        var installerPath = Environment.ProcessPath
            ?? throw new InvalidOperationException("Installer executable is not available");

        return new AbsolutePath(installerPath);
    }
    
    private static string CreateShortcutCreationScript(AbsolutePath shortcutPath, AbsolutePath installationPath)
        => $"""
            $WshShell = New-Object -ComObject WScript.Shell
            $Shortcut = $WshShell.CreateShortcut('{shortcutPath}')
            $Shortcut.TargetPath = '{installationPath}'
            $Shortcut.WorkingDirectory = '{installationPath.Parent}'
            $Shortcut.Description = 'A background utility that keeps your Downloads folder clean'
            $Shortcut.IconLocation = '{installationPath},0'
            $Shortcut.Save() 
            """;

    private static string CreateScheduledTaskCreationScript(AbsolutePath installationPath)
    {
        if (installationPath.Parent is null)
            throw new InvalidOperationException("Installation path is not available");
        
        var profileName = GetUserProfileName();
        
        return $"""
                <?xml version="1.0" encoding="UTF-16"?>
                <Task version="1.2" xmlns="http://schemas.microsoft.com/windows/2004/02/mit/task">
                  <RegistrationInfo>
                    <Description>Starts {UserFriendlyApplicationName} at user logon</Description>
                  </RegistrationInfo>
                  <Triggers>
                    <LogonTrigger>
                      <Enabled>true</Enabled>
                      <UserId>{profileName}</UserId>
                    </LogonTrigger>
                  </Triggers>
                  <Principals>
                    <Principal id="Author">
                      <UserId>{profileName}</UserId>
                      <LogonType>InteractiveToken</LogonType>
                      <RunLevel>LeastPrivilege</RunLevel>
                    </Principal>
                  </Principals>
                  <Settings>
                    <MultipleInstancesPolicy>IgnoreNew</MultipleInstancesPolicy>
                    <DisallowStartIfOnBatteries>false</DisallowStartIfOnBatteries>
                    <StopIfGoingOnBatteries>false</StopIfGoingOnBatteries>
                    <AllowHardTerminate>true</AllowHardTerminate>
                    <StartWhenAvailable>true</StartWhenAvailable>
                    <RunOnlyIfNetworkAvailable>false</RunOnlyIfNetworkAvailable>
                    <IdleSettings>
                      <StopOnIdleEnd>false</StopOnIdleEnd>
                      <RestartOnIdle>false</RestartOnIdle>
                    </IdleSettings>
                    <AllowStartOnDemand>true</AllowStartOnDemand>
                    <Enabled>true</Enabled>
                    <Hidden>false</Hidden>
                    <RunOnlyIfIdle>false</RunOnlyIfIdle>
                    <WakeToRun>false</WakeToRun>
                    <ExecutionTimeLimit>PT0S</ExecutionTimeLimit>
                    <Priority>7</Priority>
                  </Settings>
                  <Actions Context="Author">
                    <Exec>
                      <Command>{installationPath}</Command>
                      <WorkingDirectory>{installationPath.Parent.Value}</WorkingDirectory>
                    </Exec>
                  </Actions>
                </Task>
                """;
    }

    private static string CreateOpeningScript(AbsolutePath installationPath)
        => $"-Command \"Start-Sleep -Seconds 1; Start-Process '{installationPath}'\"";

    private static string GetUserProfileName()
        => $"{Environment.UserDomainName}\\{Environment.UserName}";
}