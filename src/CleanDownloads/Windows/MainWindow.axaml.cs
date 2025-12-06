using System;
using Avalonia;
using Avalonia.Controls;

namespace CleanDownloads.Windows;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        
        Deactivated += OnDeactivated;
    }

    private void OnDeactivated(object? sender, EventArgs eventArgs)
    {
        Hide();
    }

    protected override void OnOpened(EventArgs eventArgs)
    {
        base.OnOpened(eventArgs);
        
        Measure(Size.Infinity);
        
        var screen = Screens.ScreenFromWindow(this)!;
        var x = screen.WorkingArea.Width - Bounds.Width * screen.Scaling;
        var y = screen.WorkingArea.Height - Bounds.Height * screen.Scaling;

        Position = new PixelPoint((int)(x - x / 10.0), (int)y);
    }
}