using System;
using UniMcp.Models;

namespace UniMcp.Executer
{
    /// <summary>
    /// Handles synchronous function calls from the MCP server.
    /// Single-shot call: pass func + args, get result in the same response (no id/type like async_call).
    /// </summary>
    public class SyncCall : ToolsCall
    {
        public SyncCall()
        {
            SetToolName("sync_call");
        }

        /// <summary>
        /// Handler for sync function calls. Expects { "func": "method_name", "args": { ... } }.
        /// </summary>
        public override void HandleCommand(JsonNode cmd, Action<JsonNode> callback)
        {
            try
            {
                string func = cmd["func"]?.Value;
                JsonClass args = cmd["args"]?.AsObject;

                if (string.IsNullOrWhiteSpace(func))
                {
                    callback(Response.Error("Required parameter 'func' is missing or empty."));
                    return;
                }

                McpLogger.Log($"[SyncCall] Executing function synchronously: {func}");
                InvokeMethodStatic(func, args, callback);
            }
            catch (Exception e)
            {
                McpLogger.LogError($"[SyncCall] Command execution failed: {e}");
                callback(Response.Error($"Internal error processing sync call: {e.Message}"));
            }
        }
    }
}
