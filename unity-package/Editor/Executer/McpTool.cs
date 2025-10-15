// Migrated from Newtonsoft.Json to SimpleJson
using System.Threading.Tasks;

namespace UnityMcp.Executer
{
    public abstract class McpTool
    {
        public abstract string ToolName { get; }
        /// <summary>
        /// Process command（Synchronous version，Remain backward compatible）
        /// </summary>
        /// <param name="ctx">Command parameters（JSONNode Type，Can be JsonClass Or other type）</param>
        /// <returns>Process result</returns>
        public abstract void HandleCommand(JsonNode ctx, System.Action<JsonNode> callback);
    }
}