using System;
using System.Reactive.Disposables;
using Avalonia;
using Avalonia.ReactiveUI;
using CleanDownloads.ViewModels;
using ReactiveUI;

namespace CleanDownloads.Views;

public partial class MainWindow : ReactiveWindow<MainWindowViewModel>
{
    public MainWindow()
    {
        InitializeComponent();
        
        this.WhenActivated(disposables =>
        {
            ViewModel?
                .CloseCommand
                .Subscribe(_ => Hide())
                .DisposeWith(disposables);
            
            ViewModel?
                .ApplyCommand
                .Subscribe(_ => Hide())
                .DisposeWith(disposables);
        });
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        
        Measure(Size.Infinity);
        
        var screen = Screens.ScreenFromWindow(this);
        
        if (screen is null)
            return;
        
        var x = screen.WorkingArea.Width - Bounds.Width * screen.Scaling;
        var y = screen.WorkingArea.Height - Bounds.Height * screen.Scaling;

        Position = new PixelPoint((int)(x - x / 70.0), (int)(y - y / 90.0));
    }
}