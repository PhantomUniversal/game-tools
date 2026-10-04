using Aion2Tools.Services;
using Aion2Tools.ViewModels;
using Aion2Tools.Views;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace Aion2Tools;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override async void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            base.OnFrameworkInitializationCompleted();
            return;
        }

        SettingsService.Load();
        MainViewModel viewModel = new MainViewModel();
        MainWindow window = new MainWindow();
        window.DataContext = viewModel;
        desktop.MainWindow = window;
        desktop.ShutdownRequested += OnShutdownRequested;
        base.OnFrameworkInitializationCompleted();
        await viewModel.Startup();
    }

    private void OnShutdownRequested(object? sender, ShutdownRequestedEventArgs e)
    {
        SettingsService.SaveNow();
    }
}
