using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
// Migrated from Newtonsoft.Json to SimpleJson
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityMcp.Models; // For Response class
using UnityMcp;

namespace UnityMcp.Tools
{
    /// <summary>
    /// Handles reading and clearing Unity Editor console log entries.
    /// Uses reflection to access internal LogEntry methods/properties.
    /// Corresponding method name: console_reader
    /// </summary>
    [ToolName("console_read", "Development tool")]
    public class ConsoleRead : StateMethodBase
    {
        // Note：Actual console operation features have been moved to ConsoleController

        /// <summary>
        /// Create parameter key list supported by current method
        /// </summary>
        protected override MethodKey[] CreateKeys()
        {
            return new[]
            {
                new MethodKey("action", "Operation type：get(No stack trace), get_full(Include stack trace), clear(Clear console)", false),
                new MethodKey("types", "Message type list：error, warning, log，Default to all types", true),
                new MethodKey("count", "Maximum number of messages returned，Fetch all if not set", true),
                new MethodKey("filterText", "Text filter，Filter logs containing specified text", true),
                new MethodKey("format", "Output format：plain, detailed, json，Defaultdetailed", true)
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
        // --- State Tree Action Handlers for GET (Stack trace not included) ---

        /// <summary>
        /// Handle retrieving all console logs（No filter，Stack trace not included）Operation of
        /// </summary>
        private object HandleGetAllWithoutFilter(JsonClass args)
        {
            return GetConsoleEntriesInternal(args, null, null, false, "all log entries (no filter, no stacktrace)");
        }

        /// <summary>
        /// Handle retrieving all console logs（With filter，Stack trace not included）Operation of
        /// </summary>
        private object HandleGetAllWithFilter(JsonClass args)
        {
            string filterText = args["filterText"]?.Value;
            return GetConsoleEntriesInternal(args, null, filterText, false, $"all log entries (filtered by '{filterText}', no stacktrace)");
        }

        /// <summary>
        /// Handle retrieving partial console logs（No filter，Stack trace not included）Operation of
        /// </summary>
        private object HandleGetPartialWithoutFilter(JsonClass args)
        {
            int count = args["count"].AsIntDefault(10);
            return GetConsoleEntriesInternal(args, count, null, false, $"{count} log entries (no filter, no stacktrace)");
        }

        /// <summary>
        /// Handle retrieving partial console logs（With filter，Stack trace not included）Operation of
        /// </summary>
        private object HandleGetPartialWithFilter(JsonClass args)
        {
            int count = args["count"].AsIntDefault(10);
            string filterText = args["filterText"]?.Value;
            return GetConsoleEntriesInternal(args, count, filterText, false, $"{count} log entries (filtered by '{filterText}', no stacktrace)");
        }

        // --- State Tree Action Handlers for GET_FULL (Include stack trace) ---

        /// <summary>
        /// Handle retrieving all console logs（No filter，Include stack trace）Operation of
        /// </summary>
        private object HandleGetFullAllWithoutFilter(JsonClass args)
        {
            return GetConsoleEntriesInternal(args, null, null, true, "all log entries (no filter, with stacktrace)");
        }

        /// <summary>
        /// Handle retrieving all console logs（With filter，Include stack trace）Operation of
        /// </summary>
        private object HandleGetFullAllWithFilter(JsonClass args)
        {
            string filterText = args["filterText"]?.Value;
            return GetConsoleEntriesInternal(args, null, filterText, true, $"all log entries (filtered by '{filterText}', with stacktrace)");
        }

        /// <summary>
        /// Handle retrieving partial console logs（No filter，Include stack trace）Operation of
        /// </summary>
        private object HandleGetFullPartialWithoutFilter(JsonClass args)
        {
            int count = args["count"].AsIntDefault(10);
            return GetConsoleEntriesInternal(args, count, null, true, $"{count} log entries (no filter, with stacktrace)");
        }

        /// <summary>
        /// Handle retrieving partial console logs（With filter，Include stack trace）Operation of
        /// </summary>
        private object HandleGetFullPartialWithFilter(JsonClass args)
        {
            int count = args["count"].AsIntDefault(10);
            string filterText = args["filterText"]?.Value;
            return GetConsoleEntriesInternal(args, count, filterText, true, $"{count} log entries (filtered by '{filterText}', with stacktrace)");
        }

        /// <summary>
        /// Unified console log fetching logic
        /// </summary>
        private object GetConsoleEntriesInternal(JsonClass args, int? count, string filterText, bool includeStacktrace, string description)
        {
            // Check ConsoleController Whether correctly initialized
            if (!ConsoleUtils.AreReflectionMembersInitialized())
            {
                if (McpConnect.EnableLog) Debug.LogError(
                    "[ReadConsole] GetConsoleEntriesInternal called but ConsoleController reflection members are not initialized."
                );
                return Response.Error(
                    "ConsoleController failed to initialize due to reflection errors. Cannot access console logs."
                );
            }

            try
            {
                // Extract parameter
                var types = ExtractTypes(args);
                string format = ExtractFormat(args);

                LogInfo($"[ReadConsole] Getting {description}");

                // Use ConsoleController Get console entries
                var entries = ConsoleUtils.GetConsoleEntries(types, count, filterText, format, includeStacktrace);
                return Response.Success(
                    $"Retrieved {entries.Count} log entries ({description}).",
                    entries
                );
            }
            catch (Exception e)
            {
                if (McpConnect.EnableLog) Debug.LogError($"[ReadConsole] GetConsoleEntriesInternal failed: {e}");
                return Response.Error($"Internal error processing console entries: {e.Message}");
            }
        }

        /// <summary>
        /// Handle clearing of console
        /// </summary>
        private object HandleClearAction(JsonClass args)
        {
            // Check ConsoleController Whether correctly initialized
            if (!ConsoleUtils.AreReflectionMembersInitialized())
            {
                if (McpConnect.EnableLog) Debug.LogError(
                    "[ReadConsole] HandleClearAction called but ConsoleController reflection members are not initialized."
                );
                return Response.Error(
                    "ConsoleController failed to initialize due to reflection errors. Cannot clear console logs."
                );
            }

            try
            {
                LogInfo("[ReadConsole] Clearing console logs");
                ConsoleUtils.ClearConsole();
                return Response.Success("Console cleared successfully.");
            }
            catch (Exception e)
            {
                if (McpConnect.EnableLog) Debug.LogError($"[ReadConsole] Clear action failed: {e}");
                return Response.Error($"Internal error processing clear action: {e.Message}");
            }
        }

        // --- Parameter Extraction Helper Methods ---

        /// <summary>
        /// Extract message type parameter
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
        /// Extract format parameter
        /// </summary>
        private string ExtractFormat(JsonClass args)
        {
            string format = args["format"]?.Value;
            if (string.IsNullOrEmpty(format)) format = "detailed";
            return format.ToLower();
        }

        /// <summary>
        /// Callback for handling unknown operations
        /// </summary>
        private object HandleUnknownAction(JsonClass args)
        {
            string action = args["action"]?.Value;
            if (string.IsNullOrEmpty(action)) action = "null";
            return Response.Error($"Unknown action: '{action}' for read_console. Valid actions are 'get', 'get_full', or 'clear'.");
        }

        // --- Internal Helper Methods ---

        // Note：Original console operations have been relocated to ConsoleController In
    }
}

