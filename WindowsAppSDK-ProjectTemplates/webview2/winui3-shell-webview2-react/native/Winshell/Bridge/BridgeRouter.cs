using System.Text.Json;
using System.Text.Json.Nodes;
using Winshell.Handlers;
using Microsoft.Extensions.DependencyInjection;

namespace Winshell;

public sealed class BridgeRouter
{
    private readonly Dictionary<string, Func<JsonObject?, Task<JsonNode?>>> _handlers;

    public BridgeRouter(IServiceProvider services)
    {
        _handlers = new(StringComparer.OrdinalIgnoreCase);

        // Resolve all registered IBridgeHandler instances from DI and register by their Method
        var handlers = services.GetServices<Winshell.Bridge.IBridgeHandler>();
        foreach (var h in handlers)
        {
            if (string.IsNullOrWhiteSpace(h.Method))
                continue;

            _handlers[h.Method] = h.HandleAsync;
        }
    }

    public async Task<string> HandleAsync(string requestJson)
    {
        string? id = null;

        try
        {
            var req = System.Text.Json.JsonSerializer.Deserialize<Winshell.Bridge.BridgeRequest>(requestJson)
                      ?? throw new InvalidOperationException("Invalid JSON");

            id = req.id;
            var version = req.v;
            var method = req.method;
            var @params = req.@params as JsonObject;

            if (string.IsNullOrWhiteSpace(id))
                return BridgeProtocol.ResponseError("", code: BridgeErrorCodes.InvalidRequest, message: "Missing request id");

            if (version is not null && version != BridgeProtocol.Version)
                return BridgeProtocol.ResponseError(id, code: BridgeErrorCodes.VersionNotSupported, message: $"Unsupported protocol version: {version}");

            if (string.IsNullOrWhiteSpace(method))
                return BridgeProtocol.ResponseError(id, code: BridgeErrorCodes.InvalidRequest, message: "Missing method");

            if (!_handlers.TryGetValue(method, out var handler))
                return BridgeProtocol.ResponseError(id, code: BridgeErrorCodes.MethodNotFound, message: $"Unknown method: {method}");

            var result = await handler(@params);
            return BridgeProtocol.ResponseOk(id, result);
        }
        catch (Exception ex)
        {
            return BridgeProtocol.ResponseError(id ?? "", code: BridgeErrorCodes.Exception, message: ex.Message);
        }
    }
}
