using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Winshell.Bridge
{
    public interface IBridgeHandler
    {
        string Method { get; }
        Task<JsonNode?> HandleAsync(JsonObject? @params);
    }
}
