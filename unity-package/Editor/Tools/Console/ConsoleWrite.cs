using System;
using System.Collections.Generic;
// Migrated from Newtonsoft.Json to SimpleJson
using UnityEditor;
using UnityEngine;
using UnityMcp.Models; // For Response class
using UnityMcp;

namespace UnityMcp.Tools
{
    /// <summary>
    /// Handles writing different types of log messages to Unity Editor console.
    /// Supports Error, Warning, Log, Assert, and Exception message types.
    /// Corresponding method name: console_write
    /// </summary>
    [ToolName("console_write", "Development tool")]
    public class ConsoleWrite : StateMethodBase
    {
        /// <summary>
        /// Create parameter key list supported by current method
        /// </summary>
        protected override MethodKey[] CreateKeys()
        {
            return new[]
            {
                new MethodKey("action", "Operation type：error, warning, log, assert, exception", false),
                new MethodKey("message", "Content of log message to write", false),
                new MethodKey("tag", "Log tag，Used for categorization and filtering，Optional", true),
                new MethodKey("context", "Context object name，Used to locate related in consoleGameObject，Optional", true),
                new MethodKey("condition", "Assert condition expression（Used only forassertType），Optional", true)
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
        /// Handle writing error logs
        /// </summary>
        private object HandleWriteError(JsonClass args)
        {
            try
            {
                string message = ExtractMessage(args);
                string tag = ExtractTag(args);
                UnityEngine.Object context = ExtractContext(args);

                string formattedMessage = FormatMessage(message, tag, "ERROR");

                LogInfo($"[ConsoleWrite] Writing error log: {formattedMessage}");

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
        /// Handle writing warning logs
        /// </summary>
        private object HandleWriteWarning(JsonClass args)
        {
            try
            {
                string message = ExtractMessage(args);
                string tag = ExtractTag(args);
                UnityEngine.Object context = ExtractContext(args);

                string formattedMessage = FormatMessage(message, tag, "WARNING");

                LogInfo($"[ConsoleWrite] Writing warning log: {formattedMessage}");

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
        /// Handle writing normal logs
        /// </summary>
        private object HandleWriteLog(JsonClass args)
        {
            try
            {
                string message = ExtractMessage(args);
                string tag = ExtractTag(args);
                UnityEngine.Object context = ExtractContext(args);

                string formattedMessage = FormatMessage(message, tag, "LOG");

                LogInfo($"[ConsoleWrite] Writing log: {formattedMessage}");

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
        /// Handle writing assert logs
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

                LogInfo($"[ConsoleWrite] Writing assert log: {formattedMessage}");

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
        /// Handle writing exception logs
        /// </summary>
        private object HandleWriteException(JsonClass args)
        {
            try
            {
                string message = ExtractMessage(args);
                string tag = ExtractTag(args);
                UnityEngine.Object context = ExtractContext(args);

                string formattedMessage = FormatMessage(message, tag, "EXCEPTION");

                LogInfo($"[ConsoleWrite] Writing exception log: {formattedMessage}");

                // Create a simple exception object for logging
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
        /// Callback for learning unknown operations
        /// </summary>
        private object HandleUnknownAction(JsonClass args)
        {
            string action = args["action"]?.Value;
            if (string.IsNullOrEmpty(action)) action = "null";
            return Response.Error($"Unknown action: '{action}' for console_write. Valid actions are 'error', 'warning', 'log', 'assert', or 'exception'.");
        }

        // --- Parameter Extraction Helper Methods ---

        /// <summary>
        /// Extract message parameter
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
        /// Extract tag parameter
        /// </summary>
        private string ExtractTag(JsonClass args)
        {
            return args["tag"]?.Value;
        }

        /// <summary>
        /// Extract condition parameter（For assertion）
        /// </summary>
        private string ExtractCondition(JsonClass args)
        {
            return args["condition"]?.Value;
        }

        /// <summary>
        /// Extract context object
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
                // Attempt to findGameObject
                GameObject go = GameObject.Find(contextName);
                if (go != null)
                {
                    return go;
                }

                // Attempt viaResourcesLoad resource
                UnityEngine.Object asset = Resources.Load(contextName);
                if (asset != null)
                {
                    return asset;
                }

                LogInfo($"[ConsoleWrite] Context object '{contextName}' not found, using null context");
                return null;
            }
            catch (Exception e)
            {
                LogInfo($"[ConsoleWrite] Failed to find context object '{contextName}': {e.Message}");
                return null;
            }
        }

        // --- Message Formatting Helper Methods ---

        /// <summary>
        /// Format log message
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
        /// Format assert log message
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
