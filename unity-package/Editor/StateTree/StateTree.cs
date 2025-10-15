using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;
using System.Text;
// Migrated from Newtonsoft.Json to SimpleJson

namespace UnityMcp
{
    /// <summary>
    /// StateTreeExecution context wrapper class，SupportJSONSerialized fields and non-serialized object references
    /// </summary>
    public class StateTree
    {
        public string key;                         // Variable at current layer
        public Dictionary<object, StateTree> select = new();
        public HashSet<string> optionalParams = new(); // Storage for optional parameterskey
        public Func<JsonClass, object> func;     // Leaf function（Backward compatibility，Already from JsonClass Migrate to JSONClass）
        public Func<StateTreeContext, object> contextFunc; // New context leaf function
        public const string Default = "*";          // Wildcard identifier
        public string ErrorMessage;//Execute error message

        /* Implicit conversion：Action → Leaf node（Backward compatibility，Already from JsonClass Migrate to JSONClass） */
        public static implicit operator StateTree(Func<JsonClass, object> a) => new() { func = a };

        /* Implicit conversion：ContextAction → Leaf node（New version） */
        public static implicit operator StateTree(Func<StateTreeContext, object> a) => new() { contextFunc = a };

        /* Run：Along the unique path of the tree（JSONClass Context，Backward compatibility） */
        public object Run(JsonClass ctx, Dictionary<string, object> dict = null)
        {
            // Convert toStateTreeContextAnd call the new version'sRunMethod
            StateTreeContext context = new StateTreeContext(ctx, dict ?? new Dictionary<string, object>());
            return Run(context);
        }

        /* Run：Along the unique path of the tree（StateTreeContext Context） */
        public object Run(StateTreeContext ctx)
        {
            var cur = this;
            while (cur.func == null && cur.contextFunc == null)
            {
                object keyToLookup = Default;
                StateTree next = null;

                // First check if there is a regular (matching)keyMatch
                if (!string.IsNullOrEmpty(cur.key) && ctx != null && ctx.TryGetJsonValue(cur.key, out JsonNode token))
                {
                    keyToLookup = ConvertTokenToKey(token);
                    cur.select.TryGetValue(keyToLookup, out next);
                }

                // If no regular match is found，Check optional parameters
                if (next == null && ctx != null)
                {
                    // Find all optional parameter keys
                    foreach (var kvp in cur.select)
                    {
                        if (kvp.Key == null) continue; // SkipnullKey

                        string key = kvp.Key.ToString();
                        if (!string.IsNullOrEmpty(key) && cur.optionalParams.Contains(key))
                        {
                            // Check if parameter exists and is not empty
                            if (ctx.TryGetJsonValue(key, out JsonNode paramToken) &&
                                paramToken != null &&
                                paramToken.type != JsonNodeType.Null &&
                                !string.IsNullOrEmpty(paramToken.Value))
                            {
                                next = kvp.Value;
                                break; // Use the first matching optional parameter found
                            }
                        }
                    }
                }

                // If still not found，Try default branch
                if (next == null && !cur.select.TryGetValue(Default, out next))
                {
                    var supportedKeys = cur.select.Keys
                        .Where(k => k?.ToString() != Default && !(cur.optionalParams.Contains(k?.ToString())))
                        .Select(k => k?.ToString() ?? "null")
                        .ToList();

                    // Add optional parameter to supported key list
                    var optionalKeys = cur.select.Keys
                        .Where(k => k != null && cur.optionalParams.Contains(k.ToString()))
                        .Select(k => k.ToString() + " (optional)")
                        .ToList();

                    supportedKeys.AddRange(optionalKeys);

                    string supportedKeysList = supportedKeys.Count > 0
                        ? string.Join(", ", supportedKeys)
                        : "none";

                    ErrorMessage = $"Invalid value '{keyToLookup}' for key '{cur.key}'. Supported values: [{supportedKeysList}]";
                    return null;
                }
                cur = next;
            }

            // Prefer to use new versioncontextFunc，If notfunc
            if (cur.contextFunc != null)
            {
                return cur.contextFunc.Invoke(ctx);
            }
            else if (cur.func != null)
            {
                return cur.func.Invoke(ctx?.JsonData);
            }

            return null;
        }

        private static object ConvertTokenToKey(JsonNode token)
        {
            if (token == null || token.type == JsonNodeType.Null)
                return Default;

            if (token.type == JsonNodeType.Integer)
            {
                int longVal = token.AsInt;
                if (longVal <= int.MaxValue && longVal >= int.MinValue)
                {
                    return (int)longVal;
                }
                return longVal;
            }

            if (token.type == JsonNodeType.Float)
            {
                return token.AsDouble;
            }

            if (token.type == JsonNodeType.Boolean)
            {
                return token.AsBool;
            }

            if (token.type == JsonNodeType.String)
            {
                return token.Value;
            }

            // JsonData Type directly returns value
            if (token is JsonData jsonData && !string.IsNullOrEmpty(jsonData.Value))
            {
                return jsonData.Value;
            }

            return token.Value;
        }

        /* Beautified print（Unicode Box line） */
        public void Print(StringBuilder sb, string indent = "", bool last = true, string parentEdgeLabel = null)
        {
            // Root node：Print title
            if (string.IsNullOrEmpty(indent))
            {
                sb.AppendLine($"{indent}StateTree");
            }

            // If current node has key，Print node once key（Avoid duplicating parent edge tag）
            string edgesIndent = indent;
            if (!string.IsNullOrEmpty(key) && key != parentEdgeLabel)
            {
                sb.AppendLine($"{indent}└─ {key}:");
                edgesIndent = indent + "   ";
            }

            // Enumerate and print the edges of the current node（entry.Key As edge tag）
            var entries = select.ToList();
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                bool isLastChild = i == entries.Count - 1;
                string connector = isLastChild ? "└─" : "├─";
                string label = entry.Key?.ToString() == Default ? "*" : entry.Key?.ToString();

                // If it is an optional parameter，Add(option)Identifier
                if (!string.IsNullOrEmpty(label) && optionalParams.Contains(label))
                {
                    label = label + "(option)";
                }

                if (entry.Value.func != null || entry.Value.contextFunc != null)
                {
                    string actionName;
                    if (entry.Value.contextFunc != null)
                    {
                        actionName = entry.Value.contextFunc.Method?.Name ?? "Anonymous";
                    }
                    else
                    {
                        actionName = entry.Value.func.Method?.Name ?? "Anonymous";
                    }
                    sb.AppendLine($"{edgesIndent}{connector} {label} → {actionName}");
                }
                else
                {
                    // Print edge tag
                    sb.AppendLine($"{edgesIndent}{connector} {label}");
                    // Recursively to child nodes；If the child node's key Different from the edge tag，Then print it in the subhierarchy key
                    string nextIndent = edgesIndent + (isLastChild ? "   " : "│  ");
                    entry.Value.Print(sb, nextIndent, isLastChild, label);
                }
            }
        }
        /// <summary>
        /// OverrideToStringMethod，Used to print state tree
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            Print(sb);
            return sb.ToString();
        }
    }
}