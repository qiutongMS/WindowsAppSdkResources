using System;
using System.IO;
using Microsoft.UI.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;

namespace Winshell;

public sealed partial class App : Application
{
    public static IServiceProvider? Services { get; private set; }

    public App()
    {
        ConfigureLoggingAndServices();
        InitializeComponent();
    }

    private static void ConfigureLoggingAndServices()
    {
        var logsDir = Path.Combine(AppContext.BaseDirectory, "logs");
        Directory.CreateDirectory(logsDir);

        var serilogLogger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(Path.Combine(logsDir, "winshell-.log"), rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7, shared: true)
            .CreateLogger();

        var services = new ServiceCollection();

        // Register Microsoft logging that forwards to Serilog (dispose logger when provider disposed)
        services.AddLogging(builder => builder.AddSerilog(serilogLogger, dispose: true));

        // Register handlers so they can be resolved via DI
        services.AddTransient<Winshell.Handlers.AppInfoHandler>();
        services.AddTransient<Winshell.Handlers.ClipboardGetTextHandler>();
        services.AddTransient<Winshell.Handlers.ClipboardSetTextHandler>();
        services.AddTransient<Winshell.Handlers.AiEchoHandler>();
        services.AddTransient<Winshell.Handlers.AiRemoveBackgroundHandler>();
        services.AddTransient<Winshell.Handlers.LogHandler>();

        // Register BridgeRouter as a singleton built from the service provider
        services.AddSingleton<BridgeRouter>(sp => new BridgeRouter(sp));

        Services = services.BuildServiceProvider();

        var logger = Services.GetService<ILogger<App>>();
        logger?.LogInformation("App started; logs at {LogsDir}", logsDir);
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var window = new MainWindow();
        window.Activate();
    }
}
