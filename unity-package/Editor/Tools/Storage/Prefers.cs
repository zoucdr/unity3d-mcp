using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Unity.Mcp.Models;
using Unity.Mcp;

namespace Unity.Mcp.Tools.Storage
{
    /// <summary>
    /// Handles EditorPrefs and PlayerPrefs operations for storing and retrieving preferences.
    /// 对应方法名: prefers
    /// </summary>
    [ToolName("prefers", "偏好设置管理")]
    public class Prefers : StateMethodBase
    {
        /// <summary>
        /// 创建当前方法支持的参数键列表
        /// </summary>
        protected override MethodKey[] CreateKeys()
        {
            return new[]
            {
                new MethodKey("action", "Operation type: set, get, delete, has, delete_all, get_all", false),
                new MethodKey("pref_type", "Preference type: editor or player (default: editor)", true),
                new MethodKey("key", "Preference key name", true),
                new MethodKey("value", "Value to set (for set action)", true),
                new MethodKey("value_type", "Value type: string, int, float, bool (default: string)", true),
                new MethodKey("default_value", "Default value if key doesn't exist (for get action)", true)
            };
        }

        protected override StateTree CreateStateTree()
        {
            return StateTreeBuilder
                .Create()
                .Key("action")
                    .Leaf("set", HandleSetAction)
                    .Leaf("get", HandleGetAction)
                    .Leaf("delete", HandleDeleteAction)
                    .Leaf("has", HandleHasAction)
                    .Leaf("delete_all", HandleDeleteAllAction)
                    .Leaf("get_all", HandleGetAllAction)
                .Build();
        }

        // --- Action Handlers ---

        private object HandleSetAction(JsonClass args)
        {
            string key = args["key"]?.Value;
            string value = args["value"]?.Value;
            string prefType = args["prefType"]?.Value;
            if (string.IsNullOrEmpty(prefType)) prefType = "editor";
            string valueType = args["valueType"]?.Value;
            if (string.IsNullOrEmpty(valueType)) valueType = "string";

            if (string.IsNullOrEmpty(key))
            {
                return Response.Error("'key' parameter is required for set action.");
            }

            if (value == null)
            {
                return Response.Error("'value' parameter is required for set action.");
            }

            try
            {
                bool isEditor = prefType.Equals("editor", StringComparison.OrdinalIgnoreCase);

                switch (valueType.ToLower())
                {
                    case "int":
                        if (!int.TryParse(value, out int intValue))
                        {
                            return Response.Error($"Cannot parse '{value}' as int.");
                        }
                        if (isEditor)
                            EditorPrefs.SetInt(key, intValue);
                        else
                            PlayerPrefs.SetInt(key, intValue);
                        break;

                    case "float":
                        if (!float.TryParse(value, out float floatValue))
                        {
                            return Response.Error($"Cannot parse '{value}' as float.");
                        }
                        if (isEditor)
                            EditorPrefs.SetFloat(key, floatValue);
                        else
                            PlayerPrefs.SetFloat(key, floatValue);
                        break;

                    case "bool":
                        if (!bool.TryParse(value, out bool boolValue))
                        {
                            return Response.Error($"Cannot parse '{value}' as bool.");
                        }
                        if (isEditor)
                            EditorPrefs.SetBool(key, boolValue);
                        else
                            PlayerPrefs.SetInt(key, boolValue ? 1 : 0);
                        break;

                    case "string":
                    default:
                        if (isEditor)
                            EditorPrefs.SetString(key, value);
                        else
                            PlayerPrefs.SetString(key, value);
                        break;
                }

                if (!isEditor)
                {
                    PlayerPrefs.Save();
                }

                McpLogger.Log($"[Prefers] Set {prefType}Prefs: {key} = {value} ({valueType})");
                return Response.Success($"Successfully set {prefType}Prefs key '{key}' to '{value}'.");
            }
            catch (Exception e)
            {
                McpLogger.Log($"[Prefers] Error setting preference: {e.Message}");
                return Response.Error($"Error setting preference: {e.Message}");
            }
        }

        private object HandleGetAction(JsonClass args)
        {
            string key = args["key"]?.Value;
            string prefType = args["prefType"]?.Value;
            if (string.IsNullOrEmpty(prefType)) prefType = "editor";
            string valueType = args["valueType"]?.Value;
            if (string.IsNullOrEmpty(valueType)) valueType = "string";
            string defaultValue = args["defaultValue"]?.Value;

            if (string.IsNullOrEmpty(key))
            {
                return Response.Error("'key' parameter is required for get action.");
            }

            try
            {
                bool isEditor = prefType.Equals("editor", StringComparison.OrdinalIgnoreCase);
                object result;

                switch (valueType.ToLower())
                {
                    case "int":
                        int defaultInt = defaultValue != null && int.TryParse(defaultValue, out int di) ? di : 0;
                        result = isEditor ? EditorPrefs.GetInt(key, defaultInt) : PlayerPrefs.GetInt(key, defaultInt);
                        break;

                    case "float":
                        float defaultFloat = defaultValue != null && float.TryParse(defaultValue, out float df) ? df : 0f;
                        result = isEditor ? EditorPrefs.GetFloat(key, defaultFloat) : PlayerPrefs.GetFloat(key, defaultFloat);
                        break;

                    case "bool":
                        bool defaultBool = defaultValue != null && bool.TryParse(defaultValue, out bool db) ? db : false;
                        if (isEditor)
                            result = EditorPrefs.GetBool(key, defaultBool);
                        else
                            result = PlayerPrefs.GetInt(key, defaultBool ? 1 : 0) == 1;
                        break;

                    case "string":
                    default:
                        string defaultString = defaultValue ?? string.Empty;
                        result = isEditor ? EditorPrefs.GetString(key, defaultString) : PlayerPrefs.GetString(key, defaultString);
                        break;
                }

                McpLogger.Log($"[Prefers] Get {prefType}Prefs: {key} = {result}");
                return Response.Success($"Retrieved {prefType}Prefs key '{key}'.", new { key, value = result, type = valueType });
            }
            catch (Exception e)
            {
                McpLogger.Log($"[Prefers] Error getting preference: {e.Message}");
                return Response.Error($"Error getting preference: {e.Message}");
            }
        }

        private object HandleDeleteAction(JsonClass args)
        {
            string key = args["key"]?.Value;
            string prefType = args["prefType"]?.Value;
            if (string.IsNullOrEmpty(prefType)) prefType = "editor";

            if (string.IsNullOrEmpty(key))
            {
                return Response.Error("'key' parameter is required for delete action.");
            }

            try
            {
                bool isEditor = prefType.Equals("editor", StringComparison.OrdinalIgnoreCase);

                if (isEditor)
                {
                    EditorPrefs.DeleteKey(key);
                }
                else
                {
                    PlayerPrefs.DeleteKey(key);
                    PlayerPrefs.Save();
                }

                McpLogger.Log($"[Prefers] Deleted {prefType}Prefs key: {key}");
                return Response.Success($"Successfully deleted {prefType}Prefs key '{key}'.");
            }
            catch (Exception e)
            {
                McpLogger.Log($"[Prefers] Error deleting preference: {e.Message}");
                return Response.Error($"Error deleting preference: {e.Message}");
            }
        }

        private object HandleHasAction(JsonClass args)
        {
            string key = args["key"]?.Value;
            string prefType = args["prefType"]?.Value;
            if (string.IsNullOrEmpty(prefType)) prefType = "editor";

            if (string.IsNullOrEmpty(key))
            {
                return Response.Error("'key' parameter is required for has action.");
            }

            try
            {
                bool isEditor = prefType.Equals("editor", StringComparison.OrdinalIgnoreCase);
                bool hasKey = isEditor ? EditorPrefs.HasKey(key) : PlayerPrefs.HasKey(key);

                McpLogger.Log($"[Prefers] Check {prefType}Prefs has key: {key} = {hasKey}");
                return Response.Success($"Key '{key}' exists: {hasKey}", new { key, exists = hasKey });
            }
            catch (Exception e)
            {
                McpLogger.Log($"[Prefers] Error checking preference: {e.Message}");
                return Response.Error($"Error checking preference: {e.Message}");
            }
        }

        private object HandleDeleteAllAction(JsonClass args)
        {
            string prefType = args["prefType"]?.Value;
            if (string.IsNullOrEmpty(prefType)) prefType = "editor";

            try
            {
                bool isEditor = prefType.Equals("editor", StringComparison.OrdinalIgnoreCase);

                if (isEditor)
                {
                    EditorPrefs.DeleteAll();
                    McpLogger.Log("[Prefers] Deleted all EditorPrefs");
                    return Response.Success("Successfully deleted all EditorPrefs.");
                }
                else
                {
                    PlayerPrefs.DeleteAll();
                    PlayerPrefs.Save();
                    McpLogger.Log("[Prefers] Deleted all PlayerPrefs");
                    return Response.Success("Successfully deleted all PlayerPrefs.");
                }
            }
            catch (Exception e)
            {
                McpLogger.Log($"[Prefers] Error deleting all preferences: {e.Message}");
                return Response.Error($"Error deleting all preferences: {e.Message}");
            }
        }

        private object HandleGetAllAction(JsonClass args)
        {
            string prefType = args["prefType"]?.Value;
            if (string.IsNullOrEmpty(prefType)) prefType = "editor";

            try
            {
                bool isEditor = prefType.Equals("editor", StringComparison.OrdinalIgnoreCase);

                if (isEditor)
                {
                    // EditorPrefs doesn't have a built-in way to get all keys
                    // We can only enumerate known keys or use reflection
                    McpLogger.Log("[Prefers] EditorPrefs doesn't support enumerating all keys directly");
                    return Response.Error("EditorPrefs doesn't support enumerating all keys. Use specific key names.");
                }
                else
                {
                    // PlayerPrefs also doesn't have built-in enumeration
                    McpLogger.Log("[Prefers] PlayerPrefs doesn't support enumerating all keys directly");
                    return Response.Error("PlayerPrefs doesn't support enumerating all keys. Use specific key names.");
                }
            }
            catch (Exception e)
            {
                McpLogger.Log($"[Prefers] Error getting all preferences: {e.Message}");
                return Response.Error($"Error getting all preferences: {e.Message}");
            }
        }
    }
}

