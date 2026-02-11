using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
// Migrated from Newtonsoft.Json to SimpleJson
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UniMcp.Models; // For Response class
using UniMcp;

namespace UniMcp.Tools
{
    /// <summary>
    /// Handles reading and clearing Unity Editor console log entries.
    /// Uses reflection to access internal LogEntry methods/properties.
    /// Corresponding method name: console_reader
    /// </summary>
    [ToolName("console_read", "Development Tools")]
    public class ConsoleRead : StateMethodBase
    {
        public override string Description => L.T("Console reader tool for reading and clearing Unity Editor console log entries", "控制台读取工具，用于读取和清除Unity编辑器控制台日志条目");

        // Note: Actual console operation functionality has been moved to ConsoleController

        /// <summary>
        /// Create the list of parameter keys supported by this method
        /// </summary>
        protected override MethodKey[] CreateKeys()
        {
            return new MethodKey[]
            {
                // Action type - enumeration
                new MethodStr("action", L.T("Action type", "操作类型"),false)
                    .SetEnumValues("get", "get_full", "clear"),
                
                // Message type list
                new MethodStr("types", L.T("Message type list, defaults to all types", "消息类型列表，默认为所有类型"))
                    .AddExamples("error,warning", "log"),
                
                // Maximum number of messages to return
                new MethodInt("count", L.T("Maximum number of messages to return, returns all if not set", "返回的最大消息数量，未设置则返回全部"))
                    .SetRange(1, 1000),
                
                // Text filter
                new MethodStr("filterText", L.T("Text filter, filters logs containing specified text", "文本过滤器，过滤包含指定文本的日志"))
                    .AddExamples("Error", "NullReference"),
                
                // Output format
                new MethodStr("format", L.T("Output format, defaults to detailed", "输出格式，默认为详细"))
                    .SetEnumValues("plain", "detailed", "json")
            };
        }

        protected override StateTree CreateStateTree()
        {
            return StateTreeBuilder
                .Create()
                .Key("action")
                    .Branch("get")
                        .OptionalKey("count")
                            .OptionalLeaf("filterText", HandleGetPartialWithFilter)
                            .DefaultLeaf(HandleGetPartialWithoutFilter)
                        .Up()
                        .OptionalLeaf("filterText", HandleGetAllWithFilter)
                        .DefaultLeaf(HandleGetAllWithoutFilter)
                    .Up()
                    .Branch("get_full")
                        .OptionalKey("count")
                            .OptionalLeaf("filterText", HandleGetFullPartialWithFilter)
                            .DefaultLeaf(HandleGetFullPartialWithoutFilter)
                        .Up()
                        .OptionalLeaf("filterText", HandleGetFullAllWithFilter)
                        .DefaultLeaf(HandleGetFullAllWithoutFilter)
                    .Up()
                    .Leaf("clear", HandleClearAction)
                .Build();
        }
        // --- State Tree Action Handlers for GET (不包含堆栈跟踪) ---

        /// <summary>
        /// 处理获取全部控制台日志（无过滤，不包含堆栈跟踪）的操作
        /// </summary>
        private object HandleGetAllWithoutFilter(JsonClass args)
        {
            return GetConsoleEntriesInternal(args, null, null, false, "all log entries (no filter, no stacktrace)");
        }

        /// <summary>
        /// 处理获取全部控制台日志（有过滤，不包含堆栈跟踪）的操作
        /// </summary>
        private object HandleGetAllWithFilter(JsonClass args)
        {
            string filterText = args["filterText"]?.Value;
            return GetConsoleEntriesInternal(args, null, filterText, false, $"all log entries (filtered by '{filterText}', no stacktrace)");
        }

        /// <summary>
        /// 处理获取部分控制台日志（无过滤，不包含堆栈跟踪）的操作
        /// </summary>
        private object HandleGetPartialWithoutFilter(JsonClass args)
        {
            int count = args["count"].AsIntDefault(10);
            return GetConsoleEntriesInternal(args, count, null, false, $"{count} log entries (no filter, no stacktrace)");
        }

        /// <summary>
        /// 处理获取部分控制台日志（有过滤，不包含堆栈跟踪）的操作
        /// </summary>
        private object HandleGetPartialWithFilter(JsonClass args)
        {
            int count = args["count"].AsIntDefault(10);
            string filterText = args["filterText"]?.Value;
            return GetConsoleEntriesInternal(args, count, filterText, false, $"{count} log entries (filtered by '{filterText}', no stacktrace)");
        }

        // --- State Tree Action Handlers for GET_FULL (包含堆栈跟踪) ---

        /// <summary>
        /// 处理获取全部控制台日志（无过滤，包含堆栈跟踪）的操作
        /// </summary>
        private object HandleGetFullAllWithoutFilter(JsonClass args)
        {
            return GetConsoleEntriesInternal(args, null, null, true, "all log entries (no filter, with stacktrace)");
        }

        /// <summary>
        /// 处理获取全部控制台日志（有过滤，包含堆栈跟踪）的操作
        /// </summary>
        private object HandleGetFullAllWithFilter(JsonClass args)
        {
            string filterText = args["filterText"]?.Value;
            return GetConsoleEntriesInternal(args, null, filterText, true, $"all log entries (filtered by '{filterText}', with stacktrace)");
        }

        /// <summary>
        /// 处理获取部分控制台日志（无过滤，包含堆栈跟踪）的操作
        /// </summary>
        private object HandleGetFullPartialWithoutFilter(JsonClass args)
        {
            int count = args["count"].AsIntDefault(10);
            return GetConsoleEntriesInternal(args, count, null, true, $"{count} log entries (no filter, with stacktrace)");
        }

        /// <summary>
        /// 处理获取部分控制台日志（有过滤，包含堆栈跟踪）的操作
        /// </summary>
        private object HandleGetFullPartialWithFilter(JsonClass args)
        {
            int count = args["count"].AsIntDefault(10);
            string filterText = args["filterText"]?.Value;
            return GetConsoleEntriesInternal(args, count, filterText, true, $"{count} log entries (filtered by '{filterText}', with stacktrace)");
        }

        /// <summary>
        /// 统一的控制台日志获取逻辑
        /// </summary>
        private object GetConsoleEntriesInternal(JsonClass args, int? count, string filterText, bool includeStacktrace, string description)
        {
            // 检查 ConsoleController 是否已正确初始化
            if (!ConsoleUtils.AreReflectionMembersInitialized())
            {
                McpLogger.LogError(
                    "[ReadConsole] GetConsoleEntriesInternal called but ConsoleController reflection members are not initialized."
                );
                return Response.Error(
                    "ConsoleController failed to initialize due to reflection errors. Cannot access console logs."
                );
            }

            try
            {
                // 提取参数
                var types = ExtractTypes(args);
                string format = ExtractFormat(args);

                McpLogger.Log($"[ReadConsole] Getting {description}");

                // 使用 ConsoleController 获取控制台条目
                var entries = ConsoleUtils.GetConsoleEntries(types, count, filterText, format, includeStacktrace);
                return Response.Success(
                    $"Retrieved {entries.Count} log entries ({description}).",
                    entries
                );
            }
            catch (Exception e)
            {
                McpLogger.LogError($"[ReadConsole] GetConsoleEntriesInternal failed: {e}");
                return Response.Error($"Internal error processing console entries: {e.Message}");
            }
        }

        /// <summary>
        /// 处理清空控制台的操作
        /// </summary>
        private object HandleClearAction(JsonClass args)
        {
            // 检查 ConsoleController 是否已正确初始化
            if (!ConsoleUtils.AreReflectionMembersInitialized())
            {
                McpLogger.LogError(
                    "[ReadConsole] HandleClearAction called but ConsoleController reflection members are not initialized."
                );
                return Response.Error(
                    "ConsoleController failed to initialize due to reflection errors. Cannot clear console logs."
                );
            }

            try
            {
                McpLogger.Log("[ReadConsole] Clearing console logs");
                ConsoleUtils.ClearConsole();
                return Response.Success("Console cleared successfully.");
            }
            catch (Exception e)
            {
                McpLogger.LogError($"[ReadConsole] Clear action failed: {e}");
                return Response.Error($"Internal error processing clear action: {e.Message}");
            }
        }

        // --- Parameter Extraction Helper Methods ---

        /// <summary>
        /// 提取消息类型参数
        /// </summary>
        private List<string> ExtractTypes(JsonClass args)
        {
            var typesArray = args["types"] as JsonArray;
            List<string> types;

            if (typesArray != null && typesArray.Count > 0)
            {
                types = new List<string>();
                foreach (JsonNode typeNode in typesArray.Childs)
                {
                    types.Add(typeNode.Value.ToLower());
                }
            }
            else
            {
                types = new List<string> { "error", "warning", "log" };
            }

            if (types.Contains("all"))
            {
                types = new List<string> { "error", "warning", "log" };
            }

            return types;
        }

        /// <summary>
        /// 提取格式参数
        /// </summary>
        private string ExtractFormat(JsonClass args)
        {
            string format = args["format"]?.Value;
            if (string.IsNullOrEmpty(format)) format = "detailed";
            return format.ToLower();
        }

        /// <summary>
        /// 处理未知操作的回调方法
        /// </summary>
        private object HandleUnknownAction(JsonClass args)
        {
            string action = args["action"]?.Value;
            if (string.IsNullOrEmpty(action)) action = "null";
            return Response.Error($"Unknown action: '{action}' for read_console. Valid actions are 'get', 'get_full', or 'clear'.");
        }

        // --- Internal Helper Methods ---

        // 注意：原来的控制台操作实现已移动到 ConsoleController 中
    }
}

