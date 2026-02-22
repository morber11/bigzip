using Avalonia;
using BigZipUI.Services;
using BigZipUI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace BigZipUI;

class Program
{
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    public static AppBuilder BuildAvaloniaApp()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IBigzipService, BigzipService>();
        services.AddSingleton<IDispatcher, AvaloniaDispatcher>();
        services.AddTransient<MainWindowViewModel>();

        var serviceProvider = services.BuildServiceProvider();

        return AppBuilder.Configure(() => new App(serviceProvider))
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
    }
}