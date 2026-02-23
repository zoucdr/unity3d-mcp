using UnityEngine;

namespace UniMcp.Runtime
{
    [System.Serializable]
    public class McpRequest
    {
        public string jsonrpc { get; set; } = "2.0";
        public string method { get; set; }
        public JsonNode @params { get; set; }
        public string id { get; set; }
    }
}
