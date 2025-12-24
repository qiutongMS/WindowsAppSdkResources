using System.Text.Json;
using System.Text.Json.Nodes;
using Winshell.Bridge;

namespace Winshell.Handlers;

public sealed class AiEchoHandler
{
    private readonly Microsoft.Extensions.Logging.ILogger<AiEchoHandler>? _log;

    public AiEchoHandler(Microsoft.Extensions.Logging.ILogger<AiEchoHandler>? log = null)
    {
        _log = log;
    }

    public Task<JsonNode?> HandleAsync(JsonObject? p)
    {
        var input = p?["text"]?.GetValue<string>() ?? string.Empty;
        var result = new AiEchoResult($"echo: {input}");
        return Task.FromResult(JsonSerializer.SerializeToNode(result));
    }
}
