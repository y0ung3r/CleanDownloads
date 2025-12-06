using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace CleanDownloads;

public sealed class HostLifetime : IClassicDesktopStyleApplicationLifetime, IDisposable
{
    private readonly ClassicDesktopStyleApplicationLifetime _avaloniaLifetime;
    
    public HostLifetime(ClassicDesktopStyleApplicationLifetime avaloniaLifetime)
    {
        avaloniaLifetime.Startup += OnStartup;
        avaloniaLifetime.Exit += OnExit;
        avaloniaLifetime.ShutdownRequested += OnShutdownRequested;
        
        _avaloniaLifetime = avaloniaLifetime;
    }
    
    public event EventHandler<ControlledApplicationLifetimeStartupEventArgs>? Startup;
    public event EventHandler<ControlledApplicationLifetimeExitEventArgs>? Exit;
    public event EventHandler<ShutdownRequestedEventArgs>? ShutdownRequested;
    
    public void Shutdown(int exitCode = 0)
        => _avaloniaLifetime.Shutdown(exitCode);
    
    public bool TryShutdown(int exitCode = 0)
        => _avaloniaLifetime.TryShutdown(exitCode);

    public string[]? Args 
        => _avaloniaLifetime.Args;
    
    public ShutdownMode ShutdownMode
    {
        get => _avaloniaLifetime.ShutdownMode;
        set => _avaloniaLifetime.ShutdownMode = value;
    }

    public Window? MainWindow
    {
        get => _avaloniaLifetime.MainWindow;
        set => _avaloniaLifetime.MainWindow = value;
    }

    public IReadOnlyList<Window> Windows
        => _avaloniaLifetime.Windows;

    public void Dispose()
    {
        _avaloniaLifetime.Startup -= OnStartup;
        _avaloniaLifetime.Exit -= OnExit;
        _avaloniaLifetime.ShutdownRequested -= OnShutdownRequested;
    }

    private void OnStartup(object? sender, ControlledApplicationLifetimeStartupEventArgs eventArgs) 
        => Startup?.Invoke(sender, eventArgs);

    private void OnExit(object? sender, ControlledApplicationLifetimeExitEventArgs eventArgs) 
        => Exit?.Invoke(sender, eventArgs);

    private void OnShutdownRequested(object? sender, ShutdownRequestedEventArgs eventArgs) 
        => ShutdownRequested?.Invoke(sender, eventArgs);
}