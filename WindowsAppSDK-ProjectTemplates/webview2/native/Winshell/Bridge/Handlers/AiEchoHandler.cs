using System.Text.Json;
using System.Text.Json.Nodes;
using Winshell.Bridge;

namespace Winshell.Handlers;

public sealed class AiEchoHandler
{
    public Task<JsonNode?> HandleAsync(JsonObject? p)
    {
        var input = p?["text"]?.GetValue<string>() ?? string.Empty;
        var result = new AiEchoResult($"echo: {input}");
        return Task.FromResult(JsonSerializer.SerializeToNode(result));
    }
}
