using System.Text.Json;
using System.Text.Json.Nodes;
using Windows.ApplicationModel.DataTransfer;
using Winshell.Bridge;

namespace Winshell.Handlers;

public sealed class ClipboardSetTextHandler
{
    private readonly Microsoft.Extensions.Logging.ILogger<ClipboardSetTextHandler>? _log;

    public ClipboardSetTextHandler(Microsoft.Extensions.Logging.ILogger<ClipboardSetTextHandler>? log = null)
    {
        _log = log;
    }

    public Task<JsonNode?> HandleAsync(JsonObject? p)
    {
        var text = p?["text"]?.GetValue<string>() ?? string.Empty;

        var pkg = new DataPackage();
        pkg.SetText(text);
        Clipboard.SetContent(pkg);

        return Task.FromResult(JsonSerializer.SerializeToNode(new OperationOkResult(true)));
    }
}
