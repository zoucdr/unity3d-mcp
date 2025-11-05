using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UniMcp.Models;
using UniMcp;

namespace UniMcp.Executer
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
                McpLogger.LogError($"[FunctionsCall] Command execution failed: {e}");
                callback(Response.Error($"Internal error processing batch function calls: {e.Message}"));
                return;
            }
        }

        /// <summary>
        /// Executes multiple functions sequentially and collects results (异步版本).
        /// </summary>
        private void ExecuteFunctions(JsonArray funcsArray, Action<JsonNode> callback)
        {
            McpLogger.Log($"[FunctionsCall] Executing {funcsArray.Count} function calls asynchronously");

            var results = new List<object>();
            int totalCalls = funcsArray.Count;
            int successfulCalls = 0;
            int failedCalls = 0;

            try
            {
                // 确保方法已注册
                ToolsCall.EnsureMethodsRegisteredStatic();

                // 如果没有函数调用，直接返回
                if (totalCalls == 0)
                {
                    var emptyResponse = CreateBatchResponse(true, results, totalCalls, successfulCalls, failedCalls);
                    callback(emptyResponse);
                    return;
                }

                // 初始化结果列表
                for (int i = 0; i < totalCalls; i++)
                {
                    results.Add(null);
                }

                // 开始异步顺序执行
                ExecuteFunctionAtIndex(funcsArray, 0, results, totalCalls, callback);
            }
            catch (Exception e)
            {
                callback(CreateBatchResponse(false, results, totalCalls, 0, 1,
                    $"批量调用初始化过程中发生未预期错误: {e.Message}"));
                return;
            }
        }

        /// <summary>
        /// 异步顺序执行指定索引的函数，完成后递归执行下一个.
        /// 如果遇到执行错误，将中断执行并返回错误信息
        /// </summary>
        private void ExecuteFunctionAtIndex(JsonArray funcsArray, int currentIndex, List<object> results,
            int totalCalls, Action<JsonClass> finalCallback)
        {
            // 如果所有函数都执行完毕，返回最终结果
            if (currentIndex >= totalCalls)
            {
                // 统计成功和失败的调用数
                int successfulCalls = 0;
                int failedCalls = 0;

                foreach (var result in results)
                {
                    if (result != null && result is JsonClass jsonResult)
                    {
                        // 检查结果中的success字段
                        var successNode = jsonResult["success"];

                        McpLogger.Log($"[BatchCall] Result success node: {successNode?.Value ?? "null"}, type: {successNode?.GetType().Name ?? "null"}");

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
                        // null 或无效结果视为失败
                        failedCalls++;

                        McpLogger.Log($"[BatchCall] Result is null or not JsonClass: {result?.GetType().Name ?? "null"}");
                    }
                }

                // 只有所有调用都成功时才返回 Success
                bool allSuccess = failedCalls == 0;
                var finalResponse = CreateBatchResponse(allSuccess, results, totalCalls, successfulCalls, failedCalls);
                finalCallback(finalResponse);

                McpLogger.Log($"[FunctionsCall] Batch execution completed: {successfulCalls}/{totalCalls} successful, {failedCalls} failed");
                return;
            }

            try
            {
                var funcCallToken = funcsArray[currentIndex];

                // 验证函数调用对象格式
                if (!(funcCallToken is JsonClass funcCall))
                {
                    string errorMsg = $"第{currentIndex + 1}个函数调用必须是对象类型";
                    results[currentIndex] = null;

                    McpLogger.LogError($"[FunctionsCall] {errorMsg}");

                    // 中断执行并返回错误
                    var abortResponse = CreateBatchResponse(false, results, totalCalls, currentIndex, 1, errorMsg);
                    finalCallback(abortResponse);
                    return;
                }

                // 提取func和args字段
                string funcName = funcCall["func"]?.Value;
                var argsToken = funcCall["args"];

                // 验证func字段
                if (string.IsNullOrWhiteSpace(funcName))
                {
                    string errorMsg = $"第{currentIndex + 1}个函数调用的func字段无效或为空";
                    results[currentIndex] = null;

                    McpLogger.LogError($"[FunctionsCall] {errorMsg}");

                    // 中断执行并返回错误
                    var abortResponse = CreateBatchResponse(false, results, totalCalls, currentIndex, 1, errorMsg);
                    finalCallback(abortResponse);
                    return;
                }

                // 验证args字段（应该是对象）
                if (!(argsToken is JsonClass args))
                {
                    string errorMsg = $"第{currentIndex + 1}个函数调用的args字段必须是对象类型";
                    results[currentIndex] = null;

                    McpLogger.LogError($"[FunctionsCall] {errorMsg}");

                    // 中断执行并返回错误
                    var abortResponse = CreateBatchResponse(false, results, totalCalls, currentIndex, 1, errorMsg);
                    finalCallback(abortResponse);
                    return;
                }

                // 异步执行单个函数调用
                ExecuteSingleFunctionAsync(funcName, args, (singleResult) =>
                {
                    // 保存当前函数的执行结果
                    results[currentIndex] = singleResult;

                    McpLogger.Log($"[FunctionsCall] Function {currentIndex + 1}/{totalCalls} ({funcName}) executed");

                    // 检查执行结果是否成功
                    bool isSuccess = false;
                    if (singleResult != null && singleResult is JsonClass jsonResult)
                    {
                        var successNode = jsonResult["success"];
                        isSuccess = successNode != null && successNode.Value == "true";
                    }

                    // 如果执行失败，中断后续执行
                    if (!isSuccess)
                    {
                        McpLogger.LogError($"[FunctionsCall] Function {currentIndex + 1}/{totalCalls} ({funcName}) failed, aborting batch execution");

                        // 创建中断执行的响应
                        string errorMsg = $"批量执行中断：执行到第{currentIndex + 1}个函数时遇到错误，中断执行";
                        var abortResponse = CreateBatchResponse(false, results, totalCalls, currentIndex, 1, errorMsg);
                        finalCallback(abortResponse);
                        return;
                    }

                    // 继续执行下一个函数
                    ExecuteFunctionAtIndex(funcsArray, currentIndex + 1, results, totalCalls, finalCallback);
                });
            }
            catch (Exception e)
            {
                string errorMsg = $"第{currentIndex + 1}个函数调用失败: {e.Message}";
                results[currentIndex] = null;

                McpLogger.LogError($"[FunctionsCall] {errorMsg}");

                // 中断执行并返回错误
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
                // 查找对应的工具方法
                var method = ToolsCall.GetRegisteredMethod(functionName);
                if (method == null)
                {
                    var availableMethods = string.Join(", ", ToolsCall.GetRegisteredMethodNames());
                    McpLogger.LogWarning($"BatchCall Unknown method: '{functionName}'. Available methods: {availableMethods}");
                    callback(null);
                    return;
                }

                // 创建执行上下文
                var state = new StateTreeContext(args, new Dictionary<string, object>());

                // 异步执行方法
                method.ExecuteMethod(state);
                state.RegistComplete((result) =>
                {
                    try
                    {
                        // 成功执行，调用回调并传递结果
                        callback(result);
                    }
                    catch (Exception e)
                    {
                        // 执行过程中出现异常
                        McpLogger.LogError($"[FunctionsCall] Exception in result callback for '{functionName}': {e}");
                        callback(null);
                    }
                });
            }
            catch (Exception e)
            {
                // 方法查找或执行设置过程中的异常
                McpLogger.LogError($"[FunctionsCall] Exception setting up execution for '{functionName}': {e}");
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
