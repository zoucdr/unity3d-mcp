using UnityEngine;

namespace UniMcp
{
    /// <summary>
    /// MCP响应数据结构
    /// </summary>
    [System.Serializable]
    public class McpResponse
    {
        public string jsonrpc { get; set; } = "2.0";
        public JsonNode result { get; set; }
        public JsonNode error { get; set; }
        public string id { get; set; }
    }
}
