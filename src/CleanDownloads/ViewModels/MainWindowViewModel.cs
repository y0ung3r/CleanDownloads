using System;
using System.Reactive;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace CleanDownloads.ViewModels;

public sealed partial class MainWindowViewModel : ViewModelBase
{
    private readonly ILogger<MainWindowViewModel> _logger;
    private readonly CleaningSettings _currentSettings;
    
    [Reactive]
    public partial bool DeletePermanently { get; set; }
    
    [Reactive]
    public partial bool SendToRecycleBin { get; set; }
    
    [Reactive]
    public partial bool DisableDeletion { get; set; }

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
        
        DeletePermanently = currentSettings.DeleteMode is RecycleMode.DeletePermanently;
        SendToRecycleBin = currentSettings.DeleteMode is RecycleMode.SendToRecycleBin;
        DisableDeletion = currentSettings.DeleteMode is RecycleMode.Disable;
        
        CloseCommand = ReactiveCommand.Create(() => { });
        ApplyCommand = ReactiveCommand.CreateFromTask(ApplyAsync);
    }

    private async Task ApplyAsync()
    {
        _currentSettings.DeleteMode = this switch
        {
            _ when DeletePermanently => RecycleMode.DeletePermanently,
            _ when SendToRecycleBin => RecycleMode.SendToRecycleBin,
            _ when DisableDeletion => RecycleMode.Disable
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