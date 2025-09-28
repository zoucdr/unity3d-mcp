using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityMcp.Models;
using UnityMcp;

namespace UnityMcp.Tools
{
    /// <summary>
    /// Handles method calls by routing to specific tool methods.
    /// Acts as a dispatcher that forwards calls to the appropriate IToolMethod implementations.
    /// The ToolName property is determined by the "type" field in the command.
    /// Example: {"type": "hierarchy_create", "args": {"from": "primitive", "primitive_type": "Cube", "name": "Enemy"}}
    /// </summary>
    public class MethodsCall : McpTool
    {
        private string _methodType = "methods_call";
        public override string ToolName => _methodType;

        // ������ע��Ĺ���ʵ�� (key: snake_case����, value: ����ʵ��)
        private static Dictionary<string, IToolMethod> _registeredMethods = null;
        private static readonly object _registrationLock = new object();

        internal void SetToolName(string toolName)
        {
            this._methodType = toolName;
        }

        /// <summary>
        /// ��ȡָ�����ƵĹ��߷���
        /// </summary>
        /// <param name="toolName">��������</param>
        /// <returns>���߷���ʵ�������δ�ҵ��򷵻�null</returns>
        public IToolMethod GetToolMethod(string toolName)
        {
            EnsureMethodsRegistered();
            _registeredMethods.TryGetValue(toolName, out IToolMethod method);
            return method;
        }

        /// <summary>
        /// Main handler for method calls (ͬ���汾).
        /// Expects command format: {"type": "hierarchy_create", "args": {...}}
        /// </summary>
        public override void HandleCommand(JObject args, Action<object> callback)
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

                string argsJson = args.ToString();
                ExecuteMethod(_methodType, argsJson, callback);
            }
            catch (Exception e)
            {
                if (McpConnect.EnableLog) Debug.LogError($"[MethodsCall] Command execution failed: {e}");
                callback(Response.Error($"Internal error processing method call: {e.Message}"));
                return;
            }
        }

        /// <summary>
        /// Executes a specific method by routing to the appropriate tool method (ͬ���汾).
        /// </summary>
        private void ExecuteMethod(string methodName, string argsJson, Action<object> callback)
        {
            if (McpConnect.EnableLog)
                Debug.Log($"[MethodsCall] Executing method: {methodName}->{argsJson}");
            try
            {
                // ȷ��������ע��
                EnsureMethodsRegistered();

                // ��������
                JObject args = JObject.Parse(argsJson);

                // ���Ҷ�Ӧ�Ĺ��߷���
                if (!_registeredMethods.TryGetValue(methodName, out IToolMethod method))
                {
                    callback(Response.Error($"Unknown method: '{methodName}'. Available methods: {string.Join(", ", _registeredMethods.Keys)}"));
                    return;
                }

                // ���ù��ߵ�ExecuteMethod����
                var state = new StateTreeContext(args, new System.Collections.Generic.Dictionary<string, object>());
                method.ExecuteMethod(state);
                state.RegistComplete(callback);
            }
            catch (Exception e)
            {
                if (McpConnect.EnableLog) Debug.LogError($"[MethodsCall] Failed to execute method '{methodName}': {e}");
                callback(Response.Error($"Error executing method '{methodName}->{argsJson}': {e.Message}"));
            }
        }

        /// <summary>
        /// ȷ�����з�����ͨ������ע�� (�ڲ�����)
        /// </summary>
        private void EnsureMethodsRegistered()
        {
            EnsureMethodsRegisteredStatic();
        }

        /// <summary>
        /// ȷ�����з�����ͨ������ע�� (��̬��������������ʹ��)
        /// </summary>
        public static void EnsureMethodsRegisteredStatic()
        {
            if (_registeredMethods != null) return;

            lock (_registrationLock)
            {
                if (_registeredMethods != null) return; // ˫�ؼ������

                _registeredMethods = new Dictionary<string, IToolMethod>();

                try
                {
                    // ͨ������������г�����ʵ��IToolMethod�ӿڵ���
                    var methodTypes = new List<Type>();

                    // ���������Ѽ��صĳ���
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
                            // ĳЩ���򼯿����޷���ȫ���أ������ǿ��Ի�ȡ�ɹ����ص�����
                            var loadedTypes = ex.Types.Where(t => t != null &&
                                typeof(IToolMethod).IsAssignableFrom(t) &&
                                !t.IsInterface &&
                                !t.IsAbstract).ToList();
                            methodTypes.AddRange(loadedTypes);

                            if (McpConnect.EnableLog) Debug.LogWarning($"[MethodsCall] Partial load of assembly {assembly.FullName}: {ex.Message}");
                        }
                        catch (Exception ex)
                        {
                            // �����޷����ʵĳ���
                            if (McpConnect.EnableLog) Debug.LogWarning($"[MethodsCall] Failed to load types from assembly {assembly.FullName}: {ex.Message}");
                            continue;
                        }
                    }

                    foreach (var methodType in methodTypes)
                    {
                        try
                        {
                            // ��������ʵ��
                            var methodInstance = Activator.CreateInstance(methodType) as IToolMethod;
                            if (methodInstance != null)
                            {
                                // ����ʹ��ToolNameAttributeָ�������ƣ�����ת������Ϊsnake_case��ʽ
                                string methodName = GetMethodName(methodType);
                                _registeredMethods[methodName] = methodInstance;
                                if (McpConnect.EnableLog) Debug.Log($"[MethodsCall] Registered method: {methodName} -> {methodType.FullName}");
                            }
                        }
                        catch (Exception e)
                        {
                            if (McpConnect.EnableLog) Debug.LogError($"[MethodsCall] Failed to register method {methodType.FullName}: {e}");
                        }
                    }

                    if (McpConnect.EnableLog) Debug.Log($"[MethodsCall] Total registered methods: {_registeredMethods.Count}");
                    if (McpConnect.EnableLog) Debug.Log($"[MethodsCall] Available methods: {string.Join(", ", _registeredMethods.Keys)}");
                }
                catch (Exception e)
                {
                    if (McpConnect.EnableLog) Debug.LogError($"[MethodsCall] Failed to register methods: {e}");
                    _registeredMethods = new Dictionary<string, IToolMethod>(); // ȷ����Ϊnull
                }
            }
        }

        /// <summary>
        /// ��ȡ��ע��ķ���ʵ�� (��̬��������������ʹ��)
        /// </summary>
        /// <param name="methodName">��������</param>
        /// <returns>����ʵ�������δ�ҵ��򷵻�null</returns>
        public static IToolMethod GetRegisteredMethod(string methodName)
        {
            EnsureMethodsRegisteredStatic();
            _registeredMethods.TryGetValue(methodName, out IToolMethod method);
            return method;
        }

        /// <summary>
        /// ��ȡ�������ƣ�����ʹ��ToolNameAttributeָ�������ƣ�����ת������Ϊsnake_case��ʽ
        /// </summary>
        /// <param name="methodType">��������</param>
        /// <returns>��������</returns>
        private static string GetMethodName(Type methodType)
        {
            // ����Ƿ���ToolNameAttribute
            var toolNameAttribute = methodType.GetCustomAttribute<ToolNameAttribute>();
            if (toolNameAttribute != null)
            {
                return toolNameAttribute.ToolName;
            }

            // ���˵�����ת��Ϊsnake_case��ʽ
            return ConvertToSnakeCase(methodType.Name);
        }

        /// <summary>
        /// ��Pascal������ת��Ϊsnake_case������
        /// ����: ManageAsset -> manage_asset, ExecuteMenuItem -> execute_menu_item
        /// </summary>
        private static string ConvertToSnakeCase(string pascalCase)
        {
            if (string.IsNullOrEmpty(pascalCase))
                return pascalCase;

            // ʹ��������ʽ�ڴ�д��ĸǰ�����»��ߣ�Ȼ��ת��ΪСд
            return Regex.Replace(pascalCase, "(?<!^)([A-Z])", "_$1").ToLower();
        }

        /// <summary>
        /// �ֶ�ע�᷽�������ⲿ���ã�
        /// </summary>
        public static void RegisterMethod(string methodName, IToolMethod method)
        {
            lock (_registrationLock)
            {
                if (_registeredMethods == null)
                    _registeredMethods = new Dictionary<string, IToolMethod>();

                _registeredMethods[methodName] = method;
                if (McpConnect.EnableLog) Debug.Log($"[MethodsCall] Manually registered method: {methodName}");
            }
        }

        /// <summary>
        /// ��ȡ������ע��ķ�������
        /// </summary>
        public static string[] GetRegisteredMethodNames()
        {
            EnsureMethodsRegisteredStatic();
            return _registeredMethods?.Keys.ToArray() ?? new string[0];
        }

    }
}
