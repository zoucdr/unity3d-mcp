using UnityEngine;

namespace UniMcp
{
    /// <summary>
    /// MCP工具信息类
    /// </summary>
    [System.Serializable]
    public class ToolInfo
    {
        public string name { get; set; }
        public string description { get; set; }
        public JsonNode inputSchema { get; set; }
    }
}
