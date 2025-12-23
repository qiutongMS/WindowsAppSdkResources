using System.Text.Json;
using System.Text.Json.Nodes;
using Winshell.Handlers;

namespace Winshell;

public sealed class BridgeRouter
{
    private readonly Dictionary<string, Func<JsonObject?, Task<JsonNode?>>> _handlers;

    public BridgeRouter()
    {
        _handlers = new(StringComparer.OrdinalIgnoreCase)
        {
            [BridgeMethods.AppGetInfo] = new AppInfoHandler().HandleAsync,
            [BridgeMethods.ClipboardGetText] = new ClipboardGetTextHandler().HandleAsync,
            [BridgeMethods.ClipboardSetText] = new ClipboardSetTextHandler().HandleAsync,
            [BridgeMethods.AiEcho] = new AiEchoHandler().HandleAsync,
            [BridgeMethods.AiRemoveBackground] = new AiRemoveBackgroundHandler().HandleAsync,
            [BridgeMethods.AppLog] = new LogHandler().HandleAsync,
        };
    }

    public async Task<string> HandleAsync(string requestJson)
    {
        string? id = null;

        try
        {
            var root = JsonNode.Parse(requestJson)?.AsObject() ?? throw new InvalidOperationException("Invalid JSON");
            id = root["id"]?.GetValue<string>();
            var version = root["v"]?.GetValue<int?>();
            var method = root["method"]?.GetValue<string>();
            var @params = root["params"] as JsonObject;

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
