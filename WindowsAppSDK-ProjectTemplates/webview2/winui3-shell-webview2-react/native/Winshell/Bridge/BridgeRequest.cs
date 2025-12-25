using System.Text.Json.Nodes;

namespace Winshell.Bridge
{
    public sealed class BridgeRequest
    {
        public string? id { get; set; }
        public int? v { get; set; }
        public string? method { get; set; }
        public JsonNode? @params { get; set; }
    }
}
