using System;
using System.Reactive;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualBasic.FileIO;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace CleanDownloads.ViewModels;

public sealed partial class MainWindowViewModel : ViewModelBase
{
    private readonly ILogger<MainWindowViewModel> _logger;
    private readonly CleaningSettings _currentSettings;
    
    [Reactive]
    public partial bool IsDeletePermanently { get; set; }
    
    [Reactive]
    public partial bool IsSendToRecycleBin { get; set; }

    public ReactiveCommand<Unit, Unit> CloseCommand { get; }
    
    public ReactiveCommand<Unit, Unit> ApplyCommand { get; }

    public MainWindowViewModel()
        : this(new NullLogger<MainWindowViewModel>(), new CleaningSettings())
    { }

    public MainWindowViewModel(
        ILogger<MainWindowViewModel> logger, 
        CleaningSettings currentSettings)
    {
        _logger = logger;
        _currentSettings = currentSettings;
        
        IsDeletePermanently = currentSettings.DeleteMode is RecycleOption.DeletePermanently;
        IsSendToRecycleBin = currentSettings.DeleteMode is RecycleOption.SendToRecycleBin;
        
        CloseCommand = ReactiveCommand.Create(() => { });
        ApplyCommand = ReactiveCommand.CreateFromTask(ApplyAsync);
    }

    private async Task ApplyAsync()
    {
        _currentSettings.DeleteMode = this switch
        {
            _ when IsDeletePermanently => RecycleOption.DeletePermanently,
            _ => RecycleOption.SendToRecycleBin
        };

        try
        {
            await _currentSettings.SaveAsync();

            _logger.LogInformation("Settings applied successfully. New delete mode is {DeleteMode} now", _currentSettings.DeleteMode);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to save settings");
        }
    }
}