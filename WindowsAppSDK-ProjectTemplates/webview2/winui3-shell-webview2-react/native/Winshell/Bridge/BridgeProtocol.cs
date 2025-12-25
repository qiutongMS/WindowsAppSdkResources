using System.Text.Json;
using System.Text.Json.Nodes;

namespace Winshell;

public static class BridgeProtocol
{
    public const int Version = 1;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false,
    };

    // Strongly-typed envelope so C# can be the single source of truth (and can generate TS defs).
    public record BridgeError<TDetails>(string Code, string Message, TDetails? Details);

    public record BridgeResponse<TPayload, TError>(int V, string Id, bool Ok, TPayload? Result, BridgeError<TError>? Error);

    public static string ResponseOk(string id, JsonNode? result) => ResponseOk<JsonNode?>(id, result);

    public static string ResponseError(string id, string code, string message, JsonNode? details = null) =>
        ResponseError<JsonNode?>(id, code, message, details);

    public static string ResponseOk<TPayload>(string id, TPayload? result)
    {
        var envelope = new BridgeResponse<TPayload, JsonNode?>(Version, id, true, result, null);
        return JsonSerializer.Serialize(envelope, JsonOptions);
    }

    public static string ResponseError<TError>(string id, string code, string message, TError? details = default)
    {
        var error = new BridgeError<TError>(code, message, details);
        var envelope = new BridgeResponse<JsonNode?, TError>(Version, id, false, null, error);
        return JsonSerializer.Serialize(envelope, JsonOptions);
    }
}
