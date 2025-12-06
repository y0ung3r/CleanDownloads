using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using CleanDownloads.ViewModels;

namespace CleanDownloads;

public sealed class ViewLocator : IDataTemplate
{
    private const string ViewModel = nameof(ViewModel);
    private const string View = nameof(View);
    
    public bool Match(object? data) 
        => data is ViewModelBase;
    
    public Control? Build(object? parameter)
    {
        var viewName = parameter?
            .GetType()
            .FullName?
            .Replace(ViewModel, View, StringComparison.Ordinal);

        if (viewName is null)
            return null;
        
        var viewType = Type.GetType(viewName);

        if (viewType is not null)
            return (Control?)Activator.CreateInstance(viewType);

        return new TextBlock
        {
            Text = $"Unable to find \"{viewName}\""
        };
    }
}