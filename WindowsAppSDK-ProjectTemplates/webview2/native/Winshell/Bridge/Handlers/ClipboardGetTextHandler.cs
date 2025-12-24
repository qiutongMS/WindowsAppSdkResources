using System.Text.Json;
using System.Text.Json.Nodes;
using Windows.ApplicationModel.DataTransfer;
using Winshell.Bridge;

namespace Winshell.Handlers;

public sealed class ClipboardGetTextHandler
{
    private readonly Microsoft.Extensions.Logging.ILogger<ClipboardGetTextHandler>? _log;

    public ClipboardGetTextHandler(Microsoft.Extensions.Logging.ILogger<ClipboardGetTextHandler>? log = null)
    {
        _log = log;
    }

    public Task<JsonNode?> HandleAsync(JsonObject? _)
    {
        var data = Clipboard.GetContent();
        return GetTextAsync(data);
    }

    private static async Task<JsonNode?> GetTextAsync(DataPackageView data)
    {
        if (!data.Contains(StandardDataFormats.Text))
            return JsonSerializer.SerializeToNode(new ClipboardTextResult(string.Empty));

        var text = await data.GetTextAsync();
        return JsonSerializer.SerializeToNode(new ClipboardTextResult(text));
    }
}
