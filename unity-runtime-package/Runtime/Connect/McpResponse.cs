using UnityEngine;

namespace UniMcp.Runtime.Models
{
    [System.Serializable]
    public class McpResponse
    {
        public string jsonrpc { get; set; } = "2.0";
        public JsonNode result { get; set; }
        public JsonNode error { get; set; }
        public string id { get; set; }
    }
}
