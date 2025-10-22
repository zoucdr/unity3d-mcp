using System;
using UnityEngine;
using Unity.Mcp.Models;

namespace Unity.Mcp.Executer
{
    /// <summary>
    /// Handles single function calls from MCP server.
    /// Routes function calls to appropriate tool classes via reflection and name matching.
    /// </summary>
    public class SingleCall : McpTool
    {
        public override string ToolName => "single_call";

        /// <summary>
        /// Main handler for function calls (同步版本).
        /// </summary>
        public override void HandleCommand(JsonNode cmd, Action<JsonNode> callback)
        {
            try
            {
                string functionName = cmd["func"]?.Value;
                JsonClass argsJson = cmd["args"]?.AsObject;

                if (string.IsNullOrWhiteSpace(functionName))
                {
                    callback(Response.Error("Required parameter 'func' is missing or empty."));
                    return;
                }

                ExecuteFunction(functionName, argsJson, callback);
            }
            catch (Exception e)
            {
                if (McpService.EnableLog) Debug.LogError($"[FunctionCall] Command execution failed: {e}");
                callback(Response.Error($"Internal error processing function call: {e.Message}"));
                return;
            }
        }

        /// <summary>
        /// Executes a specific function by routing to the appropriate method (同步版本).
        /// </summary>
        private void ExecuteFunction(string functionName, JsonClass args, Action<JsonNode> callback)
        {
            if (McpService.EnableLog)
                Debug.Log($"[FunctionCall] Executing function: {functionName}->{args}");
            try
            {
                // 确保方法已注册
                ToolsCall.EnsureMethodsRegisteredStatic();
                // 查找对应的工具方法
                var method = ToolsCall.GetRegisteredMethod(functionName);
                if (method == null)
                {
                    callback(Response.Error($"SingleCall Unknown method: '{functionName}'. Available methods: {string.Join(", ", ToolsCall.GetRegisteredMethodNames())}"));
                    return;
                }

                // 调用工具的ExecuteMethod方法
                var state = new StateTreeContext(args, new System.Collections.Generic.Dictionary<string, object>());
                method.ExecuteMethod(state);
                state.RegistComplete(callback);
            }
            catch (Exception e)
            {
                if (McpService.EnableLog) Debug.LogError($"[FunctionCall] Failed to execute function '{functionName}': {e}");
                callback(Response.Error($"Error executing function '{functionName}->{args}': {e.Message}"));
            }
        }
    }
}