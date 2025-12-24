using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Winshell.Bridge;

namespace Winshell.Handlers;

public sealed class LogHandler : Winshell.Bridge.IBridgeHandler
{
    private readonly ILogger<LogHandler>? _log;
    private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web);

    public LogHandler(ILogger<LogHandler>? log = null)
    {
        _log = log;
    }

    public Task<JsonNode?> HandleAsync(JsonObject? p)
    {
        var level = p?["level"]?.GetValue<string>()?.ToLowerInvariant() ?? "info";
        var message = p?["message"]?.GetValue<string>() ?? string.Empty;
        var meta = p?["meta"];

        var metaJson = meta is null ? null : meta.ToJsonString(WebJson);

        switch (level)
        {
            case "warn":
            case "warning":
                _log?.LogWarning("WEB {Message} meta={Meta}", message, metaJson);
                break;
            case "error":
                _log?.LogError("WEB {Message} meta={Meta}", message, metaJson);
                break;
            default:
                _log?.LogInformation("WEB {Message} meta={Meta}", message, metaJson);
                break;
        }

        return Task.FromResult(JsonSerializer.SerializeToNode(new OperationOkResult(true)));
    }

    public string Method => BridgeMethods.AppLog;
}
