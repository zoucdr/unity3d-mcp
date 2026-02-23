using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UniMcp.Runtime;

namespace UniMcp.Runtime.Executer
{
    public class ToolsCall : McpTool
    {
        private string _methodType = "tools_call";
        public override string ToolName => _methodType;

        private static Dictionary<string, IToolMethod> _registeredMethods = null;
        private static readonly object _registrationLock = new object();

        internal void SetToolName(string toolName)
        {
            this._methodType = toolName;
        }

        public IToolMethod GetToolMethod(string toolName)
        {
            EnsureMethodsRegistered();
            _registeredMethods.TryGetValue(toolName, out IToolMethod method);
            return method;
        }

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
                RuntimeLogger.LogError($"[ToolsCall] Command execution failed: {e}");
                callback(Response.Error($"Internal error processing method call: {e.Message}"));
                return;
            }
        }

        private void ExecuteMethod(string methodName, JsonClass args, Action<JsonNode> callback)
        {
            InvokeMethodStatic(methodName, args, callback);
        }

        public static void InvokeMethodStatic(string methodName, JsonClass args, Action<JsonNode> callback)
        {
            RuntimeLogger.Log($"[ToolsCall] Executing method: {methodName}->{args}");
            try
            {
                EnsureMethodsRegisteredStatic();
                if (!_registeredMethods.TryGetValue(methodName, out IToolMethod method))
                {
                    callback(Response.Error($"Unknown method: '{methodName}'. Available methods: {string.Join(", ", _registeredMethods.Keys)}"));
                    return;
                }

                var state = new StateTreeContext(args ?? new JsonClass(), new Dictionary<string, object>());
                method.ExecuteMethod(state);
                state.RegistComplete(callback);
            }
            catch (Exception e)
            {
                RuntimeLogger.LogError($"[ToolsCall] Failed to execute method '{methodName}': {e}");
                callback(Response.Error($"Error executing method '{methodName}': {e.Message}"));
            }
        }

        private void EnsureMethodsRegistered()
        {
            EnsureMethodsRegisteredStatic();
        }

        public static void EnsureMethodsRegisteredStatic()
        {
            if (_registeredMethods != null) return;

            lock (_registrationLock)
            {
                if (_registeredMethods != null) return;

                _registeredMethods = new Dictionary<string, IToolMethod>();

                try
                {
                    var methodTypes = new List<Type>();

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
                            var loadedTypes = ex.Types.Where(t => t != null &&
                                typeof(IToolMethod).IsAssignableFrom(t) &&
                                !t.IsInterface &&
                                !t.IsAbstract).ToList();
                            methodTypes.AddRange(loadedTypes);
                        }
                        catch (Exception)
                        {
                            continue;
                        }
                    }

                    foreach (var methodType in methodTypes)
                    {
                        try
                        {
                            var methodInstance = Activator.CreateInstance(methodType) as IToolMethod;
                            if (methodInstance != null)
                            {
                                string methodName = GetMethodName(methodType);
                                _registeredMethods[methodName] = methodInstance;
                                RuntimeLogger.Log($"[ToolsCall] Registered method: {methodName} -> {methodType.FullName}");
                            }
                        }
                        catch (Exception e)
                        {
                            RuntimeLogger.LogError($"[ToolsCall] Failed to register method {methodType.FullName}: {e}");
                        }
                    }

                    RuntimeLogger.Log($"[ToolsCall] Total registered methods: {_registeredMethods.Count}");
                }
                catch (Exception e)
                {
                    RuntimeLogger.LogError($"[ToolsCall] Failed to register methods: {e}");
                    _registeredMethods = new Dictionary<string, IToolMethod>();
                }
            }
        }

        public static IToolMethod GetRegisteredMethod(string methodName)
        {
            EnsureMethodsRegisteredStatic();
            _registeredMethods.TryGetValue(methodName, out IToolMethod method);
            return method;
        }

        private static string GetMethodName(Type methodType)
        {
            var toolNameAttribute = methodType.GetCustomAttribute<ToolNameAttribute>();
            if (toolNameAttribute != null)
            {
                return toolNameAttribute.ToolName;
            }

            return ConvertToSnakeCase(methodType.Name);
        }

        private static string ConvertToSnakeCase(string pascalCase)
        {
            if (string.IsNullOrEmpty(pascalCase))
                return pascalCase;

            return Regex.Replace(pascalCase, "(?<!^)([A-Z])", "_$1").ToLower();
        }

        public static void RegisterMethod(string methodName, IToolMethod method)
        {
            lock (_registrationLock)
            {
                if (_registeredMethods == null)
                    _registeredMethods = new Dictionary<string, IToolMethod>();

                _registeredMethods[methodName] = method;
                RuntimeLogger.Log($"[ToolsCall] Manually registered method: {methodName}");
            }
        }

        public static string[] GetRegisteredMethodNames()
        {
            EnsureMethodsRegisteredStatic();
            return _registeredMethods?.Keys.ToArray() ?? new string[0];
        }

        public static void ClearRegisteredMethods()
        {
            lock (_registrationLock)
            {
                RuntimeLogger.Log("[ToolsCall] Clearing registered methods cache");
                _registeredMethods = null;
            }
        }
    }
}
