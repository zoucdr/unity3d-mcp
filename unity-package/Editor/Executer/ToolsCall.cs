using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
// Migrated from Newtonsoft.Json to SimpleJson
using UnityEditor;
using UnityEngine;
using UnityMcp.Models;
using UnityMcp;

namespace UnityMcp.Executer
{
    /// <summary>
    /// Handles method calls by routing to specific tool methods.
    /// Acts as a dispatcher that forwards calls to the appropriate IToolMethod implementations.
    /// The ToolName property is determined by the "type" field in the command.
    /// Example: {"type": "hierarchy_create", "args": {"from": "primitive", "primitive_type": "Cube", "name": "Enemy"}}
    /// </summary>
    public class ToolsCall : McpTool
    {
        private string _methodType = "tools_call";
        public override string ToolName => _methodType;

        // Already registered tool instance (key: snake_caseName, value: Tool instance)
        private static Dictionary<string, IToolMethod> _registeredMethods = null;
        private static readonly object _registrationLock = new object();

        internal void SetToolName(string toolName)
        {
            this._methodType = toolName;
        }

        /// <summary>
        /// Get tool method by specified name
        /// </summary>
        /// <param name="toolName">Tool name</param>
        /// <returns>Tool method instance，Return if not foundnull</returns>
        public IToolMethod GetToolMethod(string toolName)
        {
            EnsureMethodsRegistered();
            _registeredMethods.TryGetValue(toolName, out IToolMethod method);
            return method;
        }

        /// <summary>
        /// Main handler for method calls (Synchronous version).
        /// Expects command format: {"type": "hierarchy_create", "args": {...}}
        /// </summary>
        public override void HandleCommand(JsonNode args, Action<JsonNode> callback)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_methodType))
                {
                    callback(Response.Error("Required parameter 'type' is missing or empty."));
                    return;
                }

                if (args == null)
                {
                    callback(Response.Error("Required parameter 'args' is missing or not an object."));
                    return;
                }
                ExecuteMethod(_methodType, args.AsObject, callback);
            }
            catch (Exception e)
            {
                if (McpConnect.EnableLog) Debug.LogError($"[ToolsCall] Command execution failed: {e}");
                callback(Response.Error($"Internal error processing method call: {e.Message}"));
                return;
            }
        }

        /// <summary>
        /// Executes a specific method by routing to the appropriate tool method (Synchronous version).
        /// </summary>
        private void ExecuteMethod(string methodName, JsonClass args, Action<JsonNode> callback)
        {
            if (McpConnect.EnableLog)
                Debug.Log($"[ToolsCall] Executing method: {methodName}->{args}");
            try
            {
                // ȷ��������ע��
                EnsureMethodsRegistered();
                // ���Ҷ�Ӧ�Ĺ��߷���
                if (!_registeredMethods.TryGetValue(methodName, out IToolMethod method))
                {
                    callback(Response.Error($"ToolsCall Unknown method: '{methodName}'. Available methods: {string.Join(", ", _registeredMethods.Keys)}"));
                    return;
                }

                // ���ù��ߵ�ExecuteMethod����
                var state = new StateTreeContext(args, new System.Collections.Generic.Dictionary<string, object>());
                method.ExecuteMethod(state);
                state.RegistComplete(callback);
            }
            catch (Exception e)
            {
                if (McpConnect.EnableLog) Debug.LogError($"[ToolsCall] Failed to execute method '{methodName}': {e}");
                callback(Response.Error($"Error executing method '{methodName}->{args}': {e.Message}"));
            }
        }

        /// <summary>
        /// Ensure all methods are registered by reflection (Internal method)
        /// </summary>
        private void EnsureMethodsRegistered()
        {
            EnsureMethodsRegisteredStatic();
        }

        /// <summary>
        /// Ensure all methods are registered by reflection (Static method，For external invocation)
        /// </summary>
        public static void EnsureMethodsRegisteredStatic()
        {
            if (_registeredMethods != null) return;

            lock (_registrationLock)
            {
                if (_registeredMethods != null) return; // Double-check locking

                _registeredMethods = new Dictionary<string, IToolMethod>();

                try
                {
                    // Find all implementations in all assemblies via reflectionIToolMethodClass implementing the interface
                    var methodTypes = new List<Type>();

                    // Iterate through all loaded assemblies
                    foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        try
                        {
                            var types = assembly.GetTypes()
                                .Where(t => typeof(IToolMethod).IsAssignableFrom(t) &&
                                           !t.IsInterface &&
                                           !t.IsAbstract)
                                .ToList();
                            methodTypes.AddRange(types);
                        }
                        catch (ReflectionTypeLoadException ex)
                        {
                            // Some assemblies may not load completely，But we can obtain the successfully loaded types
                            var loadedTypes = ex.Types.Where(t => t != null &&
                                typeof(IToolMethod).IsAssignableFrom(t) &&
                                !t.IsInterface &&
                                !t.IsAbstract).ToList();
                            methodTypes.AddRange(loadedTypes);

                            if (McpConnect.EnableLog) Debug.LogWarning($"[ToolsCall] Partial load of assembly {assembly.FullName}: {ex.Message}");
                        }
                        catch (Exception ex)
                        {
                            // Ignore inaccessible assemblies
                            if (McpConnect.EnableLog) Debug.LogWarning($"[ToolsCall] Failed to load types from assembly {assembly.FullName}: {ex.Message}");
                            continue;
                        }
                    }

                    foreach (var methodType in methodTypes)
                    {
                        try
                        {
                            // Create method instance
                            var methodInstance = Activator.CreateInstance(methodType) as IToolMethod;
                            if (methodInstance != null)
                            {
                                // Preferred useToolNameAttributeSpecified name，Otherwise convert class name tosnake_caseForm
                                string methodName = GetMethodName(methodType);
                                _registeredMethods[methodName] = methodInstance;
                                if (McpConnect.EnableLog) Debug.Log($"[ToolsCall] Registered method: {methodName} -> {methodType.FullName}");
                            }
                        }
                        catch (Exception e)
                        {
                            if (McpConnect.EnableLog) Debug.LogError($"[ToolsCall] Failed to register method {methodType.FullName}: {e}");
                        }
                    }

                    if (McpConnect.EnableLog) Debug.Log($"[ToolsCall] Total registered methods: {_registeredMethods.Count}");
                    if (McpConnect.EnableLog) Debug.Log($"[ToolsCall] Available methods: {string.Join(", ", _registeredMethods.Keys)}");
                }
                catch (Exception e)
                {
                    if (McpConnect.EnableLog) Debug.LogError($"[ToolsCall] Failed to register methods: {e}");
                    _registeredMethods = new Dictionary<string, IToolMethod>(); // Ensure notnull
                }
            }
        }

        /// <summary>
        /// Get registered method instance (Static method，For external invocation)
        /// </summary>
        /// <param name="methodName">Method name</param>
        /// <returns>Method instance，Return if not foundnull</returns>
        public static IToolMethod GetRegisteredMethod(string methodName)
        {
            EnsureMethodsRegisteredStatic();
            _registeredMethods.TryGetValue(methodName, out IToolMethod method);
            return method;
        }

        /// <summary>
        /// Get method name，Preferred useToolNameAttributeSpecified name，Otherwise convert class name tosnake_caseForm
        /// </summary>
        /// <param name="methodType">Method type</param>
        /// <returns>Method name</returns>
        private static string GetMethodName(Type methodType)
        {
            // Check if there isToolNameAttribute
            var toolNameAttribute = methodType.GetCustomAttribute<ToolNameAttribute>();
            if (toolNameAttribute != null)
            {
                return toolNameAttribute.ToolName;
            }

            // Otherwise convert class name tosnake_caseForm
            return ConvertToSnakeCase(methodType.Name);
        }

        /// <summary>
        /// WillPascalNaming convention conversion tosnake_caseNaming convention
        /// For example: ManageAsset -> manage_asset, ExecuteMenuItem -> execute_menu_item
        /// </summary>
        private static string ConvertToSnakeCase(string pascalCase)
        {
            if (string.IsNullOrEmpty(pascalCase))
                return pascalCase;

            // Use regex to add underscores before uppercase letters，Then convert to lowercase
            return Regex.Replace(pascalCase, "(?<!^)([A-Z])", "_$1").ToLower();
        }

        /// <summary>
        /// Manually register method（For external call）
        /// </summary>
        public static void RegisterMethod(string methodName, IToolMethod method)
        {
            lock (_registrationLock)
            {
                if (_registeredMethods == null)
                    _registeredMethods = new Dictionary<string, IToolMethod>();

                _registeredMethods[methodName] = method;
                if (McpConnect.EnableLog) Debug.Log($"[ToolsCall] Manually registered method: {methodName}");
            }
        }

        /// <summary>
        /// Get all registered method names
        /// </summary>
        public static string[] GetRegisteredMethodNames()
        {
            EnsureMethodsRegisteredStatic();
            return _registeredMethods?.Keys.ToArray() ?? new string[0];
        }

    }
}
