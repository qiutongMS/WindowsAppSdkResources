using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Winshell.Bridge;
using Windows.ApplicationModel;

namespace Winshell.Handlers;

public sealed class AppInfoHandler
{
    private readonly Microsoft.Extensions.Logging.ILogger<AppInfoHandler>? _log;

    public AppInfoHandler(Microsoft.Extensions.Logging.ILogger<AppInfoHandler>? log = null)
    {
        _log = log;
    }

    public Task<JsonNode?> HandleAsync(JsonObject? _)
    {
        try
        {
            var version = Package.Current?.Id?.Version.ToString() ?? string.Empty;
            var result = new AppInfoResult(
                Name: Package.Current?.DisplayName ?? "Winshell",
                Version: version,
                Packaged: true);
            return Task.FromResult(JsonSerializer.SerializeToNode(result));
        }
        catch
        {
            var asm = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
            var fallback = new AppInfoResult(
                Name: asm.GetName().Name ?? "Winshell",
                Version: asm.GetName().Version?.ToString() ?? "0.0.0",
                Packaged: false);
            return Task.FromResult(JsonSerializer.SerializeToNode(fallback));
        }
    }
}
