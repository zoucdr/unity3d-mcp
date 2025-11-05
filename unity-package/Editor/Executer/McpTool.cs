// Migrated from Newtonsoft.Json to SimpleJson
using System.Threading.Tasks;

namespace UniMcp.Executer
{
    public abstract class McpTool
    {
        public abstract string ToolName { get; }
        /// <summary>
        /// 处理命令（同步版本，保持向后兼容）
        /// </summary>
        /// <param name="ctx">命令参数（JSONNode 类型，可以是 JsonClass 或其他类型）</param>
        /// <returns>处理结果</returns>
        public abstract void HandleCommand(JsonNode ctx, System.Action<JsonNode> callback);
    }
}