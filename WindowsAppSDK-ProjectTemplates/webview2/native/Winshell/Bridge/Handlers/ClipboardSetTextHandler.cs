using System.Text.Json;
using System.Text.Json.Nodes;
using Windows.ApplicationModel.DataTransfer;
using Winshell.Bridge;

namespace Winshell.Handlers;

public sealed class ClipboardSetTextHandler
{
    public Task<JsonNode?> HandleAsync(JsonObject? p)
    {
        var text = p?["text"]?.GetValue<string>() ?? string.Empty;

        var pkg = new DataPackage();
        pkg.SetText(text);
        Clipboard.SetContent(pkg);

        return Task.FromResult(JsonSerializer.SerializeToNode(new OperationOkResult(true)));
    }
}
