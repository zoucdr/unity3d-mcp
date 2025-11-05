using System;
using System.Collections.Generic;
using UnityEngine;
using UniMcp.Models;

namespace UniMcp.Executer
{
    /// <summary>
    /// Handles asynchronous function calls from the MCP server.
    /// It can initiate a call and store its context, or retrieve the result of a completed call.
    /// </summary>
    public class AsyncCall : McpTool
    {
        private static readonly Dictionary<string, StateTreeContext> pendingTasks = new Dictionary<string, StateTreeContext>();

        public override string ToolName => "async_call";

        /// <summary>
        /// Main handler for async function calls.
        /// </summary>
        public override void HandleCommand(JsonNode cmd, Action<JsonNode> callback)
        {
            try
            {
                string id = cmd["id"]?.Value;
                string type = cmd["type"]?.Value;

                if (string.IsNullOrWhiteSpace(id))
                {
                    callback(Response.Error("Required parameter 'id' is missing or empty."));
                    return;
                }

                if (type == "in")
                {
                    string functionName = cmd["func"]?.Value;
                    JsonClass argsJson = cmd["args"]?.AsObject;

                    if (string.IsNullOrWhiteSpace(functionName))
                    {
                        callback(Response.Error("Required parameter 'func' is missing or empty for type 'in'."));
                        return;
                    }
                    ExecuteFunctionAsync(id, functionName, argsJson, callback);
                }
                else if (type == "out")
                {
                    RetrieveResult(id, callback);
                }
                else
                {
                    callback(Response.Error($"Invalid 'type' parameter: {type}. Must be 'in' or 'out'."));
                }
            }
            catch (Exception e)
            {
                McpLogger.LogError($"[AsyncCall] Command execution failed: {e}");
                callback(Response.Error($"Internal error processing async call: {e.Message}"));
            }
        }

        private void ExecuteFunctionAsync(string id, string functionName, JsonClass args, Action<JsonNode> callback)
        {
            if (pendingTasks.ContainsKey(id))
            {
                callback(Response.Error($"Task with id '{id}' is already running."));
                return;
            }

            McpLogger.Log($"[AsyncCall] Executing function asynchronously: {functionName} with id: {id}");
            try
            {
                ToolsCall.EnsureMethodsRegisteredStatic();
                var method = ToolsCall.GetRegisteredMethod(functionName);
                if (method == null)
                {
                    callback(Response.Error($"AsyncCall Unknown method: '{functionName}'. Available methods: {string.Join(", ", ToolsCall.GetRegisteredMethodNames())}"));
                    return;
                }

                var state = new StateTreeContext(args, new Dictionary<string, object>());
                pendingTasks[id] = state;

                method.ExecuteMethod(state);

                state.RegistComplete(result =>
                {
                    // The task is complete, and the result is stored in the state.
                    // The client will poll for it using type='out'.
                    state.Result = result; // Make sure the result is stored.
                });

                // Acknowledge that the task has started.
                callback(Response.Success($"Task '{id}' started for function '{functionName}'."));
            }
            catch (Exception e)
            {
                McpLogger.LogError($"[AsyncCall] Failed to start async function '{functionName}': {e}");
                pendingTasks.Remove(id); // Clean up on failure
                callback(Response.Error($"Error starting async function '{functionName}': {e.Message}"));
            }
        }

        private void RetrieveResult(string id, Action<JsonNode> callback)
        {
            if (pendingTasks.TryGetValue(id, out var state))
            {
                if (state.IsComplete)
                {
                    McpLogger.Log($"[AsyncCall] Retrieving result for completed task id: {id}");
                    callback(state.Result ?? Response.Success("Task completed with no return value."));
                    pendingTasks.Remove(id); // Result retrieved, remove task.
                }
                else
                {
                    McpLogger.Log($"[AsyncCall] Task with id '{id}' is still running.");
                    callback(Response.Success("Task is still in progress."));
                }
            }
            else
            {
                callback(Response.Error($"No task found with id '{id}'. It might have been completed and retrieved, or never started."));
            }
        }
    }
}