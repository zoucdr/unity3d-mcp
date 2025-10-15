using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityMcp.Models;
using UnityMcp;

namespace UnityMcp.Executer
{
    /// <summary>
    /// Handles batch function calls from MCP server.
    /// Executes multiple function calls sequentially and collects all results.
    /// </summary>
    public class BatchCall : McpTool
    {
        public override string ToolName => "batch_call";

        /// <summary>
        /// Main handler for batch function calls.
        /// </summary>
        public override void HandleCommand(JsonNode cmd, Action<JsonNode> callback)
        {
            try
            {
                var funcsArray = cmd.AsArray;

                if (funcsArray == null)
                {
                    callback(Response.Error("Required parameter 'funcs' is missing or not an array."));
                    return;
                }

                ExecuteFunctions(funcsArray, callback);
            }
            catch (Exception e)
            {
                if (McpConnect.EnableLog) Debug.LogError($"[FunctionsCall] Command execution failed: {e}");
                callback(Response.Error($"Internal error processing batch function calls: {e.Message}"));
                return;
            }
        }

        /// <summary>
        /// Executes multiple functions sequentially and collects results (Asynchronous version).
        /// </summary>
        private void ExecuteFunctions(JsonArray funcsArray, Action<JsonNode> callback)
        {
            if (McpConnect.EnableLog)
                Debug.Log($"[FunctionsCall] Executing {funcsArray.Count} function calls asynchronously");

            var results = new List<object>();
            int totalCalls = funcsArray.Count;
            int successfulCalls = 0;
            int failedCalls = 0;

            try
            {
                // Ensure the method is registered
                ToolsCall.EnsureMethodsRegisteredStatic();

                // If there are no function calls，Return directly
                if (totalCalls == 0)
                {
                    var emptyResponse = CreateBatchResponse(true, results, totalCalls, successfulCalls, failedCalls);
                    callback(emptyResponse);
                    return;
                }

                // Initialize the result list
                for (int i = 0; i < totalCalls; i++)
                {
                    results.Add(null);
                }

                // Start asynchronous sequential execution
                ExecuteFunctionAtIndex(funcsArray, 0, results, totalCalls, callback);
            }
            catch (Exception e)
            {
                callback(CreateBatchResponse(false, results, totalCalls, 0, 1,
                    $"Unexpected error during batch call initialization: {e.Message}"));
                return;
            }
        }

        /// <summary>
        /// Asynchronously execute function at specified index in sequence，Recursively execute the next after completion.
        /// If execution error encountered，Execution will be interrupted and error returned
        /// </summary>
        private void ExecuteFunctionAtIndex(JsonArray funcsArray, int currentIndex, List<object> results,
            int totalCalls, Action<JsonClass> finalCallback)
        {
            // If all functions are executed，Return the final result
            if (currentIndex >= totalCalls)
            {
                // Count successful and failed calls
                int successfulCalls = 0;
                int failedCalls = 0;

                foreach (var result in results)
                {
                    if (result != null && result is JsonClass jsonResult)
                    {
                        // Check in the resultsuccessField
                        var successNode = jsonResult["success"];

                        if (McpConnect.EnableLog)
                        {
                            Debug.Log($"[BatchCall] Result success node: {successNode?.Value ?? "null"}, type: {successNode?.GetType().Name ?? "null"}");
                        }

                        if (successNode != null && successNode.Value == "true")
                        {
                            successfulCalls++;
                        }
                        else
                        {
                            failedCalls++;
                        }
                    }
                    else
                    {
                        // null Or invalid result as failure
                        failedCalls++;

                        if (McpConnect.EnableLog)
                        {
                            Debug.Log($"[BatchCall] Result is null or not JsonClass: {result?.GetType().Name ?? "null"}");
                        }
                    }
                }

                // Only returns if all calls succeed Success
                bool allSuccess = failedCalls == 0;
                var finalResponse = CreateBatchResponse(allSuccess, results, totalCalls, successfulCalls, failedCalls);
                finalCallback(finalResponse);

                if (McpConnect.EnableLog)
                    Debug.Log($"[FunctionsCall] Batch execution completed: {successfulCalls}/{totalCalls} successful, {failedCalls} failed");
                return;
            }

            try
            {
                var funcCallToken = funcsArray[currentIndex];

                // Validate function call object format
                if (!(funcCallToken is JsonClass funcCall))
                {
                    string errorMsg = $"No.{currentIndex + 1}Each function call must be an object";
                    results[currentIndex] = null;

                    if (McpConnect.EnableLog) Debug.LogError($"[FunctionsCall] {errorMsg}");

                    // Interrupt execution and return error
                    var abortResponse = CreateBatchResponse(false, results, totalCalls, currentIndex, 1, errorMsg);
                    finalCallback(abortResponse);
                    return;
                }

                // ExtractfuncAndargsField
                string funcName = funcCall["func"]?.Value;
                var argsToken = funcCall["args"];

                // ValidatefuncField
                if (string.IsNullOrWhiteSpace(funcName))
                {
                    string errorMsg = $"No.{currentIndex + 1}Function call offuncField is invalid or empty";
                    results[currentIndex] = null;

                    if (McpConnect.EnableLog) Debug.LogError($"[FunctionsCall] {errorMsg}");

                    // Interrupt execution and return error
                    var abortResponse = CreateBatchResponse(false, results, totalCalls, currentIndex, 1, errorMsg);
                    finalCallback(abortResponse);
                    return;
                }

                // ValidateargsField（Should be object）
                if (!(argsToken is JsonClass args))
                {
                    string errorMsg = $"No.{currentIndex + 1}Function call ofargsThe field must be an object";
                    results[currentIndex] = null;

                    if (McpConnect.EnableLog) Debug.LogError($"[FunctionsCall] {errorMsg}");

                    // Interrupt execution and return error
                    var abortResponse = CreateBatchResponse(false, results, totalCalls, currentIndex, 1, errorMsg);
                    finalCallback(abortResponse);
                    return;
                }

                // Asynchronously execute a single function call
                ExecuteSingleFunctionAsync(funcName, args, (singleResult) =>
                {
                    // Save current function's execution result
                    results[currentIndex] = singleResult;

                    if (McpConnect.EnableLog)
                    {
                        Debug.Log($"[FunctionsCall] Function {currentIndex + 1}/{totalCalls} ({funcName}) executed");
                    }

                    // Check if the execution result is successful
                    bool isSuccess = false;
                    if (singleResult != null && singleResult is JsonClass jsonResult)
                    {
                        var successNode = jsonResult["success"];
                        isSuccess = successNode != null && successNode.Value == "true";
                    }

                    // If execution failed，Interrupt subsequent execution
                    if (!isSuccess)
                    {
                        if (McpConnect.EnableLog)
                        {
                            Debug.LogError($"[FunctionsCall] Function {currentIndex + 1}/{totalCalls} ({funcName}) failed, aborting batch execution");
                        }

                        // Create response for interrupted execution
                        string errorMsg = $"Batch execution interrupted：Executed to{currentIndex + 1}Error when executing function，Interrupt execution";
                        var abortResponse = CreateBatchResponse(false, results, totalCalls, currentIndex, 1, errorMsg);
                        finalCallback(abortResponse);
                        return;
                    }

                    // Continue to next function
                    ExecuteFunctionAtIndex(funcsArray, currentIndex + 1, results, totalCalls, finalCallback);
                });
            }
            catch (Exception e)
            {
                string errorMsg = $"No.{currentIndex + 1}Function call failed: {e.Message}";
                results[currentIndex] = null;

                if (McpConnect.EnableLog) Debug.LogError($"[FunctionsCall] {errorMsg}");

                // Interrupt execution and return error
                var abortResponse = CreateBatchResponse(false, results, totalCalls, currentIndex, 1, errorMsg);
                finalCallback(abortResponse);
            }
        }

        /// <summary>
        /// Executes a single function asynchronously with callback.
        /// </summary>
        private void ExecuteSingleFunctionAsync(string functionName, JsonClass args, Action<object> callback)
        {
            try
            {
                // Find the corresponding tool method
                var method = ToolsCall.GetRegisteredMethod(functionName);
                if (method == null)
                {
                    var availableMethods = string.Join(", ", ToolsCall.GetRegisteredMethodNames());
                    if (McpConnect.EnableLog)
                        Debug.LogWarning($"BatchCall Unknown method: '{functionName}'. Available methods: {availableMethods}");
                    callback(null);
                    return;
                }

                // Create execution context
                var state = new StateTreeContext(args, new Dictionary<string, object>());

                // Asynchronous execution method
                method.ExecuteMethod(state);
                state.RegistComplete((result) =>
                {
                    try
                    {
                        // Executed successfully，Invoke callback and pass result
                        callback(result);
                    }
                    catch (Exception e)
                    {
                        // Exception occurred during execution
                        if (McpConnect.EnableLog) Debug.LogError($"[FunctionsCall] Exception in result callback for '{functionName}': {e}");
                        callback(null);
                    }
                });
            }
            catch (Exception e)
            {
                // Exception during method lookup or execution setup
                if (McpConnect.EnableLog) Debug.LogError($"[FunctionsCall] Exception setting up execution for '{functionName}': {e}");
                callback(null);
            }
        }

        /// <summary>
        /// Creates the batch response in the format expected by the Python layer.
        /// </summary>
        private JsonClass CreateBatchResponse(bool success, List<object> results,
            int totalCalls, int successfulCalls, int failedCalls, string globalError = null)
        {
            var responseData = new JsonClass();
            responseData.Add("results", Json.FromObject(results));
            responseData.Add("total_calls", new JsonData(totalCalls.ToString()));
            responseData.Add("successful_calls", new JsonData(successfulCalls.ToString()));
            responseData.Add("failed_calls", new JsonData(failedCalls.ToString()));

            if (!string.IsNullOrEmpty(globalError))
            {
                responseData.Add("error", new JsonData(globalError));
            }

            if (success)
            {
                return Response.Success("Batch function calls completed", responseData);
            }
            else
            {
                return Response.Error("Batch function calls Failed", responseData);
            }
        }
    }
}
