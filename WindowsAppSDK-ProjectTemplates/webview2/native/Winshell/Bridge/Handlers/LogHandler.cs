using System.Text.Json;
using System.Text.Json.Nodes;
using Serilog;
using Winshell.Bridge;

namespace Winshell.Handlers;

public sealed class LogHandler
{
    private static readonly ILogger Log = Serilog.Log.ForContext<LogHandler>();
    private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web);

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
                Log.Warning("WEB {Message} meta={Meta}", message, metaJson);
                break;
            case "error":
                Log.Error("WEB {Message} meta={Meta}", message, metaJson);
                break;
            default:
                Log.Information("WEB {Message} meta={Meta}", message, metaJson);
                break;
        }

        return Task.FromResult(JsonSerializer.SerializeToNode(new OperationOkResult(true)));
    }
}
