using System;
using System.IO;
using Microsoft.UI.Xaml;
using Serilog;

namespace Winshell;

public sealed partial class App : Application
{
    public App()
    {
        ConfigureLogging();
        InitializeComponent();
    }

    private static void ConfigureLogging()
    {
        var logsDir = Path.Combine(AppContext.BaseDirectory, "logs");
        Directory.CreateDirectory(logsDir);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(Path.Combine(logsDir, "winshell-.log"), rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7, shared: true)
            .CreateLogger();

        Log.Information("App started; logs at {LogsDir}", logsDir);

        AppDomain.CurrentDomain.ProcessExit += (_, _) => Log.CloseAndFlush();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var window = new MainWindow();
        window.Activate();
    }
}
