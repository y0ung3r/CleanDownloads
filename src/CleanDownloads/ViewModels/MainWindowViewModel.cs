using System.Reactive;
using System.Threading.Tasks;
using Microsoft.VisualBasic.FileIO;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace CleanDownloads.ViewModels;

public sealed partial class MainWindowViewModel : ViewModelBase
{
    private CleaningSettings CurrentSettings { get; }
    
    [Reactive]
    public partial bool IsDeletePermanently { get; set; }
    
    [Reactive]
    public partial bool IsSendToRecycleBin { get; set; }

    public ReactiveCommand<Unit, Unit> CloseCommand { get; }
    
    public ReactiveCommand<Unit, Unit> ApplyCommand { get; }

    public MainWindowViewModel()
        : this(new CleaningSettings())
    { }

    public MainWindowViewModel(CleaningSettings settings)
    {
        CurrentSettings = settings;
        IsDeletePermanently = settings.DeleteMode is RecycleOption.DeletePermanently;
        IsSendToRecycleBin = settings.DeleteMode is RecycleOption.SendToRecycleBin;
        CloseCommand = ReactiveCommand.Create(() => { });
        ApplyCommand = ReactiveCommand.CreateFromTask(ApplyAsync);
    }

    private async Task ApplyAsync()
    {
        CurrentSettings.DeleteMode = this switch
        {
            _ when IsDeletePermanently => RecycleOption.DeletePermanently,
            _ => RecycleOption.SendToRecycleBin
        };

        await CurrentSettings.SaveAsync();
    }
}