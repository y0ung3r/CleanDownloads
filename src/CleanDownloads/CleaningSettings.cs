using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualBasic.FileIO;
using Tomlyn;
using TruePath;

namespace CleanDownloads;

public sealed class CleaningSettings
{
    public static readonly AbsolutePath DefaultPath 
        = new(Path.Combine(AppContext.BaseDirectory, "settings.toml"));

    private string CurrentPath
    {
        get => field ?? throw new InvalidOperationException("Failed to retrieve path to cleaning settings");
        set;
    }

    public static async Task<CleaningSettings> LoadAsync(AbsolutePath settingsPath)
    {
        var absolutePath = settingsPath.ToString();

        try
        {
            if (!File.Exists(absolutePath))
                return new CleaningSettings();

            var settings = Toml.ToModel<CleaningSettings>(await File.ReadAllTextAsync(absolutePath));
            
            settings.CurrentPath = absolutePath;
            
            return settings;
        }
        catch (Exception)
        {
            return new CleaningSettings();
        }
    }

    public CleaningSettings()
        : this(DefaultPath.ToString())
    { }

    private CleaningSettings(string settingsPath)
        => CurrentPath = settingsPath;
    
    public RecycleOption DeleteMode { get; set; } 
        = RecycleOption.SendToRecycleBin;

    public async Task SaveAsync()
    {
        var tomlText = Toml.FromModel(this);
        await File.WriteAllTextAsync(CurrentPath, tomlText);
    }
}