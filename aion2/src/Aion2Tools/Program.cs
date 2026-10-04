using System;
using Aion2Tools.Services;
using Avalonia;

namespace Aion2Tools;

public sealed class Program
{
    /// <summary>No Avalonia or SynchronizationContext use before AppMain: nothing is initialized yet.</summary>
    [STAThread]
    public static void Main(string[] args)
    {
        SelfUpdate.FinishPrevious(args);
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    /// <summary>Also used by the visual designer.</summary>
    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
    }
}
