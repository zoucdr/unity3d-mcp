using UnityEngine;

namespace UniMcp.Runtime
{
    [System.Serializable]
    public class ToolInfo
    {
        public string name { get; set; }
        public string description { get; set; }
        public JsonNode inputSchema { get; set; }
    }
}
