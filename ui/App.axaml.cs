using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using BigZipUI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace BigZipUI;

public partial class App : Application
{
    private readonly IServiceProvider? _services;

    // required by Avalonia runtime loader
    public App()
    {
    }

    public App(IServiceProvider services)
    {
        _services = services;
    }
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            if (_services is not null)
            {
                var vm = _services.GetRequiredService<MainWindowViewModel>();
                desktop.MainWindow = new MainWindow
                {
                    DataContext = vm
                };
            }
            else
            {
                desktop.MainWindow = new MainWindow();
            }
        }

        base.OnFrameworkInitializationCompleted();
    }
}