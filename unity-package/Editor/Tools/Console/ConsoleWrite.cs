using System;
using System.Collections.Generic;
// Migrated from Newtonsoft.Json to SimpleJson
using UnityEditor;
using UnityEngine;
using UniMcp.Models; // For Response class
using UniMcp;

namespace UniMcp.Tools
{
    /// <summary>
    /// Handles writing different types of log messages to Unity Editor console.
    /// Supports Error, Warning, Log, Assert, and Exception message types.
    /// Corresponding method name: console_write
    /// </summary>
    [ToolName("console_write", "Development Tools")]
    public class ConsoleWrite : StateMethodBase
    {
        public override string Description => L.T("Console writer tool for writing log messages to Unity Editor console", "控制台写入工具，用于向Unity编辑器控制台写入日志消息");

        /// <summary>
        /// Create the list of parameter keys supported by this method
        /// </summary>
        protected override MethodKey[] CreateKeys()
        {
            return new MethodKey[]
            {
                // Action type - enumeration
                new MethodStr("action", L.T("Action type", "操作类型"), false)
                    .SetEnumValues("error", "warning", "log", "assert", "exception"),
                
                // Log message content - required
                new MethodStr("message", L.T("Log message content to write", "要写入的日志消息内容"), false)
                    .AddExamples("This is a test message", "Operation completed successfully"),
                
                // Log tag
                new MethodStr("tag", L.T("Log tag for categorization and filtering", "用于分类和过滤的日志标签"))
                    .AddExamples("System", "Network"),
                
                // Context object
                new MethodStr("context", L.T("Context object name, used to locate related GameObject in console", "上下文对象名称，用于在控制台中定位相关游戏对象"))
                    .AddExamples("Player", "Main Camera"),
                
                // Assert condition
                new MethodStr("condition", L.T("Assert condition expression (only for assert type)", "断言条件表达式（仅用于断言类型）"))
                    .AddExamples("value != null", "count > 0")
            };
        }

        protected override StateTree CreateStateTree()
        {
            return StateTreeBuilder
                .Create()
                .Key("action")
                    .Leaf("error", HandleWriteError)
                    .Leaf("warning", HandleWriteWarning)
                    .Leaf("log", HandleWriteLog)
                    .Leaf("assert", HandleWriteAssert)
                    .Leaf("exception", HandleWriteException)
                    .DefaultLeaf(HandleUnknownAction)
                .Build();
        }

        // --- State Tree Action Handlers ---

        /// <summary>
        /// 处理写入错误日志的操作
        /// </summary>
        private object HandleWriteError(JsonClass args)
        {
            try
            {
                string message = ExtractMessage(args);
                string tag = ExtractTag(args);
                UnityEngine.Object context = ExtractContext(args);

                string formattedMessage = FormatMessage(message, tag, "ERROR");

                McpLogger.Log($"[ConsoleWrite] Writing error log: {formattedMessage}");

                if (context != null)
                {
                    Debug.LogError(formattedMessage, context);
                }
                else
                {
                    Debug.LogError(formattedMessage);
                }

                return Response.Success($"Error log written successfully: {formattedMessage}");
            }
            catch (Exception e)
            {
                LogError($"[ConsoleWrite] Failed to write error log: {e.Message}");
                return Response.Error($"Failed to write error log: {e.Message}");
            }
        }

        /// <summary>
        /// 处理写入警告日志的操作
        /// </summary>
        private object HandleWriteWarning(JsonClass args)
        {
            try
            {
                string message = ExtractMessage(args);
                string tag = ExtractTag(args);
                UnityEngine.Object context = ExtractContext(args);

                string formattedMessage = FormatMessage(message, tag, "WARNING");

                McpLogger.Log($"[ConsoleWrite] Writing warning log: {formattedMessage}");

                if (context != null)
                {
                    Debug.LogWarning(formattedMessage, context);
                }
                else
                {
                    Debug.LogWarning(formattedMessage);
                }

                return Response.Success($"Warning log written successfully: {formattedMessage}");
            }
            catch (Exception e)
            {
                LogError($"[ConsoleWrite] Failed to write warning log: {e.Message}");
                return Response.Error($"Failed to write warning log: {e.Message}");
            }
        }

        /// <summary>
        /// 处理写入普通日志的操作
        /// </summary>
        private object HandleWriteLog(JsonClass args)
        {
            try
            {
                string message = ExtractMessage(args);
                string tag = ExtractTag(args);
                UnityEngine.Object context = ExtractContext(args);

                string formattedMessage = FormatMessage(message, tag, "LOG");

                McpLogger.Log($"[ConsoleWrite] Writing log: {formattedMessage}");

                if (context != null)
                {
                    Debug.Log(formattedMessage, context);
                }
                else
                {
                    Debug.Log(formattedMessage);
                }

                return Response.Success($"Log written successfully: {formattedMessage}");
            }
            catch (Exception e)
            {
                LogError($"[ConsoleWrite] Failed to write log: {e.Message}");
                return Response.Error($"Failed to write log: {e.Message}");
            }
        }

        /// <summary>
        /// 处理写入断言日志的操作
        /// </summary>
        private object HandleWriteAssert(JsonClass args)
        {
            try
            {
                string message = ExtractMessage(args);
                string tag = ExtractTag(args);
                string condition = ExtractCondition(args);
                UnityEngine.Object context = ExtractContext(args);

                string formattedMessage = FormatAssertMessage(message, tag, condition);

                McpLogger.Log($"[ConsoleWrite] Writing assert log: {formattedMessage}");

                if (context != null)
                {
                    Debug.LogAssertion(formattedMessage, context);
                }
                else
                {
                    Debug.LogAssertion(formattedMessage);
                }

                return Response.Success($"Assertion log written successfully: {formattedMessage}");
            }
            catch (Exception e)
            {
                LogError($"[ConsoleWrite] Failed to write assertion log: {e.Message}");
                return Response.Error($"Failed to write assertion log: {e.Message}");
            }
        }

        /// <summary>
        /// 处理写入异常日志的操作
        /// </summary>
        private object HandleWriteException(JsonClass args)
        {
            try
            {
                string message = ExtractMessage(args);
                string tag = ExtractTag(args);
                UnityEngine.Object context = ExtractContext(args);

                string formattedMessage = FormatMessage(message, tag, "EXCEPTION");

                McpLogger.Log($"[ConsoleWrite] Writing exception log: {formattedMessage}");

                // 创建一个简单的异常对象用于日志
                var exception = new System.Exception(formattedMessage);

                if (context != null)
                {
                    Debug.LogException(exception, context);
                }
                else
                {
                    Debug.LogException(exception);
                }

                return Response.Success($"Exception log written successfully: {formattedMessage}");
            }
            catch (Exception e)
            {
                LogError($"[ConsoleWrite] Failed to write exception log: {e.Message}");
                return Response.Error($"Failed to write exception log: {e.Message}");
            }
        }

        /// <summary>
        /// 处理未知操作的回调方法
        /// </summary>
        private object HandleUnknownAction(JsonClass args)
        {
            string action = args["action"]?.Value;
            if (string.IsNullOrEmpty(action)) action = "null";
            return Response.Error($"Unknown action: '{action}' for console_write. Valid actions are 'error', 'warning', 'log', 'assert', or 'exception'.");
        }

        // --- Parameter Extraction Helper Methods ---

        /// <summary>
        /// 提取消息参数
        /// </summary>
        private string ExtractMessage(JsonClass args)
        {
            string message = args["message"]?.Value;
            if (string.IsNullOrEmpty(message))
            {
                throw new ArgumentException("Message parameter is required and cannot be empty.");
            }
            return message;
        }

        /// <summary>
        /// 提取标签参数
        /// </summary>
        private string ExtractTag(JsonClass args)
        {
            return args["tag"]?.Value;
        }

        /// <summary>
        /// 提取条件参数（用于断言）
        /// </summary>
        private string ExtractCondition(JsonClass args)
        {
            return args["condition"]?.Value;
        }

        /// <summary>
        /// 提取上下文对象
        /// </summary>
        private UnityEngine.Object ExtractContext(JsonClass args)
        {
            string contextName = args["context"]?.Value;
            if (string.IsNullOrEmpty(contextName))
            {
                return null;
            }

            try
            {
                // 尝试找到GameObject
                GameObject go = GameObject.Find(contextName);
                if (go != null)
                {
                    return go;
                }

                // 尝试通过Resources加载资源
                UnityEngine.Object asset = Resources.Load(contextName);
                if (asset != null)
                {
                    return asset;
                }

                McpLogger.Log($"[ConsoleWrite] Context object '{contextName}' not found, using null context");
                return null;
            }
            catch (Exception e)
            {
                McpLogger.Log($"[ConsoleWrite] Failed to find context object '{contextName}': {e.Message}");
                return null;
            }
        }

        // --- Message Formatting Helper Methods ---

        /// <summary>
        /// 格式化日志消息
        /// </summary>
        private string FormatMessage(string message, string tag, string logType)
        {
            if (!string.IsNullOrEmpty(tag))
            {
                return $"[{tag}][{logType}] {message}";
            }
            return $"[MCP][{logType}] {message}";
        }

        /// <summary>
        /// 格式化断言日志消息
        /// </summary>
        private string FormatAssertMessage(string message, string tag, string condition)
        {
            string baseMessage = FormatMessage(message, tag, "ASSERT");

            if (!string.IsNullOrEmpty(condition))
            {
                return $"{baseMessage} | Condition: {condition}";
            }

            return baseMessage;
        }
    }
}
