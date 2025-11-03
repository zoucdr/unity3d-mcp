using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;
using Unity.Mcp.Models;
using Unity.Mcp.Executer;
using System.Collections;
using System.Reflection;

namespace Unity.Mcp
{
    [InitializeOnLoad]
    public partial class McpService
    {
        // 单例实例
        private static McpService _instance;

        // 单例访问器
        public static McpService Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new McpService();
                }
                return _instance;
            }
        }

        private HttpListener listener;
        private bool isRunning = false;
        private readonly object lockObj = new();

        // 添加取消令牌源，用于优雅地取消异步操作
        private CancellationTokenSource cancellationTokenSource;

        // 保存监听任务的引用，防止被垃圾回收
        private Task listenerTask;
        // 移除EditorPrefs相关的常量，改用McpLocalSettings

        public static int mcpPort
        {
            get
            {
                return McpLocalSettings.Instance.McpServerPort;
            }
            set
            {
                McpLocalSettings.Instance.McpServerPort = value;
            }
        }

        /// <summary>
        /// 检查工具数量是否发生变化
        /// </summary>
        private bool HasToolCountChanged()
        {
            int currentCount = availableTools.Count;
            int lastCount = McpLocalSettings.Instance.LastToolCount;

            Log($"[Unity.Mcp] 工具数量检查 - 当前: {currentCount}, 上次: {lastCount}");

            return McpLocalSettings.Instance.HasToolCountChanged(currentCount);
        }

        public bool IsRunning => isRunning;

        /// <summary>
        /// 检查监听任务是否正在运行
        /// </summary>
        public bool IsListenerTaskRunning()
        {
            return listenerTask != null &&
                   !listenerTask.IsCompleted &&
                   !listenerTask.IsFaulted &&
                   !listenerTask.IsCanceled;
        }

        /// <summary>
        /// 获取监听任务状态信息
        /// </summary>
        public string GetListenerTaskStatus()
        {
            if (listenerTask == null)
            {
                return "监听任务未创建";
            }

            if (listenerTask.IsCompleted)
            {
                return "监听任务已完成";
            }

            if (listenerTask.IsFaulted)
            {
                return $"监听任务出错: {listenerTask.Exception?.Message}";
            }

            if (listenerTask.IsCanceled)
            {
                return "监听任务已取消";
            }

            return "监听任务正在运行";
        }

        /// <summary>
        /// 获取监听器状态信息（静态方法）
        /// </summary>
        public static string GetListenerStatus()
        {
            return Instance.GetListenerTaskStatus();
        }

        // MCP协议相关
        private readonly Dictionary<string, IToolMethod> availableTools = new();
        private readonly Dictionary<string, ToolInfo> toolInfos = new();
        private readonly Dictionary<string, IPrompts> availablePrompts = new();
        private readonly Dictionary<string, IRes> availableResources = new();
        private string serverName = "Unity MCP Server";
        private string serverVersion = "1.0.0";

        // MCP工具实例缓存
        private readonly Dictionary<string, McpTool> mcpToolInstanceCache = new();
        private readonly ToolsCall methodsCall = new();

        // 消息队列处理
        private readonly Queue<Action> messageQueue = new();
        private readonly object queueLock = new();
        private bool isUpdateRegistered = false;

        // HTTP请求记录跟踪 - 使用McpExecuteRecordObject
        public int ConnectedClientCount
        {
            get
            {
                return McpExecuteRecordObject.instance.GetHttpRequestRecords().Count;
            }
        }

        [InitializeOnLoadMethod]
        static void AutoInit()
        {
            // 确保在编辑器中运行时，McpService 的静态构造函数被调用
            if (Application.isEditor)
            {
                _ = Instance;
            }
        }

        // 实例析构函数
        ~McpService()
        {
            AssemblyReloadEvents.beforeAssemblyReload -= ForceStop;
            AssemblyReloadEvents.afterAssemblyReload -= OnAfterAssemblyReload;
            ForceStop();

            // 确保取消令牌被释放
            try
            {
                cancellationTokenSource?.Cancel();
                cancellationTokenSource?.Dispose();
            }
            catch { }
        }

        // 静态访问器，方便外部调用
        public static int GetConnectedClientCount()
        {
            return Instance.ConnectedClientCount;
        }

        public List<McpExecuteRecordObject.HttpRequestRecord> GetConnectedClients()
        {
            return McpExecuteRecordObject.instance.GetHttpRequestRecords();
        }

        // 静态访问器，方便外部调用
        public static List<McpExecuteRecordObject.HttpRequestRecord> GetAllConnectedClients()
        {
            return Instance.GetConnectedClients();
        }

        /// <summary>
        /// 验证端口是否有效
        /// </summary>
        /// <param name="port">端口号</param>
        /// <returns>是否有效</returns>
        public static bool IsValidPort(int port)
        {
            return McpLocalSettings.IsValidPort(port);
        }

        /// <summary>
        /// 获取本地设置实例（方便外部访问）
        /// </summary>
        public static McpLocalSettings GetLocalSettings()
        {
            return McpLocalSettings.Instance;
        }

        /// <summary>
        /// 获取设置摘要信息（用于调试）
        /// </summary>
        public static string GetSettingsSummary()
        {
            return McpLocalSettings.Instance.GetSettingsSummary();
        }

        /// <summary>
        /// 设置MCP服务器端口
        /// </summary>
        /// <param name="port">端口号</param>
        /// <returns>设置是否成功</returns>
        public static bool SetMcpPort(int port)
        {
            if (!McpLocalSettings.IsValidPort(port))
            {
                return false;
            }

            // 如果服务正在运行且端口发生变化，需要重启服务
            bool needRestart = Instance.IsRunning && mcpPort != port;

            mcpPort = port;

            if (needRestart)
            {
                StopService();
                System.Threading.Thread.Sleep(500); // 等待停止完成
                StartService();
            }

            return true;
        }

        /// <summary>
        /// 手动重新发现工具（用于调试）
        /// </summary>
        public static void RediscoverTools()
        {
            Instance.DiscoverTools();
        }

        /// <summary>
        /// 获取当前注册的工具数量（所有工具）
        /// </summary>
        public static int GetToolCount()
        {
            return Instance.availableTools.Count;
        }

        /// <summary>
        /// 获取启用的工具数量
        /// </summary>
        public static int GetEnabledToolCount()
        {
            var allToolNames = Instance.availableTools.Keys.ToList();
            return McpLocalSettings.Instance.GetEnabledToolCount(allToolNames);
        }

        /// <summary>
        /// 获取所有工具名称
        /// </summary>
        public static List<string> GetAllToolNames()
        {
            return Instance.availableTools.Keys.ToList();
        }

        /// <summary>
        /// 获取启用的工具名称
        /// </summary>
        public static List<string> GetEnabledToolNames()
        {
            var allToolNames = Instance.availableTools.Keys.ToList();
            // 额外添加 batch_call 和 async_call
            allToolNames.Add("batch_call");
            allToolNames.Add("async_call");
            return McpLocalSettings.Instance.FilterEnabledTools(allToolNames);
        }

        /// <summary>
        /// 获取工具数量变化状态（用于调试）
        /// </summary>
        public static bool GetToolCountChangeStatus()
        {
            return Instance.HasToolCountChanged();
        }

        /// <summary>
        /// 强制重置工具数量记录（用于测试）
        /// </summary>
        public static void ResetToolCountRecord()
        {
            McpLocalSettings.Instance.ResetToolCountRecord();
            Instance.Log("[Unity.Mcp] 工具数量记录已重置");
        }

        /// <summary>
        /// 清理超过指定时间的旧请求记录
        /// </summary>
        /// <param name="maxAge">最大保留时间（分钟）</param>
        public void CleanupOldRecords(int maxAge = 30)
        {
            var oldCount = McpExecuteRecordObject.instance.GetHttpRequestRecords().Count;
            McpExecuteRecordObject.instance.CleanupOldHttpRequestRecords(maxAge);
            var newCount = McpExecuteRecordObject.instance.GetHttpRequestRecords().Count;

            if (oldCount > newCount)
            {
                Log($"[Unity.Mcp] 清理了 {oldCount - newCount} 条旧请求记录");
            }
        }

        /// <summary>
        /// 将任务添加到消息队列
        /// </summary>
        private void EnqueueTask(Action task)
        {
            lock (queueLock)
            {
                messageQueue.Enqueue(task);

                // 如果update还没有注册，则注册它
                if (!isUpdateRegistered)
                {
                    EditorApplication.update += ProcessMessageQueue;
                    isUpdateRegistered = true;
                    Log("[Unity.Mcp] 消息队列处理器已注册到EditorApplication.update");
                }
            }
        }

        /// <summary>
        /// 处理消息队列中的任务
        /// </summary>
        private void ProcessMessageQueue()
        {
            Action task = null;

            lock (queueLock)
            {
                if (messageQueue.Count > 0)
                {
                    task = messageQueue.Dequeue();
                }

                // 如果队列为空，注销update回调
                if (messageQueue.Count == 0 && isUpdateRegistered)
                {
                    EditorApplication.update -= ProcessMessageQueue;
                    isUpdateRegistered = false;
                    Log("[Unity.Mcp] 消息队列处理器已从EditorApplication.update注销");
                }
            }

            // 在锁外执行任务
            if (task != null)
            {
                try
                {
                    task.Invoke();
                }
                catch (Exception ex)
                {
                    LogError($"[Unity.Mcp] 消息队列任务执行失败: {ex.Message}\n{ex.StackTrace}");
                }
            }
        }

        // 统一的日志输出方法
        private void Log(string message)
        {
            McpLogger.Log(message);
        }

        private void LogWarning(string message)
        {
            McpLogger.LogWarning(message);
        }

        private void LogError(string message)
        {
            McpLogger.LogError(message);
        }

        public static bool FolderExists(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            if (path.Equals("Assets", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            string fullPath = Path.Combine(
                Application.dataPath,
                path.StartsWith("Assets/") ? path[7..] : path
            );
            return Directory.Exists(fullPath);
        }

        // 私有构造函数，防止外部创建实例
        private McpService()
        {
            // 初始化工具发现
            DiscoverTools();
            DiscoverPrompts();
            DiscoverResources();

            // 初始化实例
            if (McpLocalSettings.Instance.McpOpenState)
            {
                CoroutineRunner.StartCoroutine(StartServiceDelay());
            }
            //监听程序集刷新事件
            AssemblyReloadEvents.beforeAssemblyReload += ForceStop;
            AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;
        }

        /// <summary>
        /// 程序集刷新完成后的处理
        /// </summary>
        private static void OnAfterAssemblyReload()
        {
            // 程序集刷新后，重新初始化并检查是否需要自动启动服务
            if (McpLocalSettings.Instance.McpOpenState)
            {
                McpLogger.Log("[Unity.Mcp] 程序集刷新完成，检测到MCP服务状态为开启，将在1秒后自动启动服务");

                // 延迟1秒启动服务
                EditorApplication.delayCall += () =>
                {
                    // 再次检查状态，确保在延迟期间状态没有改变
                    if (McpLocalSettings.Instance.McpOpenState && !Instance.IsRunning)
                    {
                        McpLogger.Log("[Unity.Mcp] 自动启动MCP服务");
                        StartService();
                    }
                };
            }
            else
            {
                McpLogger.Log("[Unity.Mcp] 程序集刷新完成，MCP服务状态为关闭，不自动启动");
            }
        }

        /// <summary>
        /// 通过反射发现所有可用的工具
        /// </summary>
        private void DiscoverTools()
        {
            Log("[Unity.Mcp] 开始发现工具...");
            // 清空现有工具
            availableTools.Clear();
            toolInfos.Clear();

            try
            {
                // 手动添加 async_call 和 batch_call 工具
                AddAsyncMcpTool();
                AddBatchMcpTool();

                // 获取所有程序集
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                Log($"[Unity.Mcp] 检查 {assemblies.Length} 个程序集");

                // 查找所有实现IToolMethod接口的类型
                var toolTypes = assemblies
                    .SelectMany(assembly =>
                    {
                        try
                        {
                            var types = assembly.GetTypes();
                            Log($"[Unity.Mcp] 程序集 {assembly.GetName().Name} 包含 {types.Length} 个类型");
                            return types;
                        }
                        catch (Exception ex)
                        {
                            LogWarning($"[Unity.Mcp] 无法获取程序集 {assembly.GetName().Name} 的类型: {ex.Message}");
                            return new Type[0];
                        }
                    })
                    .Where(type => !type.IsAbstract &&
                                   !type.IsInterface &&
                                   typeof(IToolMethod).IsAssignableFrom(type))
                    .ToList();

                Log($"[Unity.Mcp] 找到 {toolTypes.Count} 个实现IToolMethod接口的类型");

                foreach (var toolType in toolTypes)
                {
                    try
                    {
                        Log($"[Unity.Mcp] 尝试创建工具实例: {toolType.FullName}");

                        // 创建工具实例
                        var toolInstance = Activator.CreateInstance(toolType) as IToolMethod;
                        if (toolInstance == null)
                        {
                            LogWarning($"[Unity.Mcp] 无法将 {toolType.Name} 转换为IToolMethod");
                            continue;
                        }

                        // 获取工具名称
                        string toolName = GetToolName(toolType);
                        Log($"[Unity.Mcp] 工具名称: {toolName}");

                        // 注册工具
                        availableTools[toolName] = toolInstance;

                        // 创建工具信息
                        var toolInfo = CreateToolInfo(toolName, toolInstance);
                        toolInfos[toolName] = toolInfo;

                        Log($"[Unity.Mcp] 成功注册工具: {toolName} ({toolType.Name})");
                    }
                    catch (Exception ex)
                    {
                        LogError($"[Unity.Mcp] 创建工具实例失败 {toolType.Name}: {ex.Message}\n{ex.StackTrace}");
                    }
                }

                Log($"[Unity.Mcp] 工具发现完成，共发现 {availableTools.Count} 个工具");

                // 更新工具数量到McpLocalSettings
                McpLocalSettings.Instance.LastToolCount = availableTools.Count;
                Log($"[Unity.Mcp] 已更新工具数量到McpLocalSettings: {availableTools.Count}");

                // 列出所有注册的工具
                foreach (var toolName in availableTools.Keys)
                {
                    Log($"[Unity.Mcp] 已注册工具: {toolName}");
                }
            }
            catch (Exception ex)
            {
                LogError($"[Unity.Mcp] 工具发现过程中发生错误: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// 通过反射发现所有可用的Prompts
        /// </summary>
        private void DiscoverPrompts()
        {
            Log("[Unity.Mcp] 开始发现Prompts...");
            // 清空现有prompts
            availablePrompts.Clear();

            try
            {
                // 获取所有程序集
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                Log($"[Unity.Mcp] 检查 {assemblies.Length} 个程序集中的Prompts");

                // 查找所有实现IPrompts接口的类型
                var promptTypes = assemblies
                    .SelectMany(assembly =>
                    {
                        try
                        {
                            return assembly.GetTypes();
                        }
                        catch (Exception ex)
                        {
                            LogWarning($"[Unity.Mcp] 无法获取程序集 {assembly.GetName().Name} 的类型: {ex.Message}");
                            return new Type[0];
                        }
                    })
                    .Where(type => !type.IsAbstract &&
                                   !type.IsInterface &&
                                   typeof(IPrompts).IsAssignableFrom(type))
                    .ToList();

                Log($"[Unity.Mcp] 找到 {promptTypes.Count} 个实现IPrompts接口的类型");

                foreach (var promptType in promptTypes)
                {
                    try
                    {
                        Log($"[Unity.Mcp] 尝试创建Prompt实例: {promptType.FullName}");

                        // 创建prompt实例
                        var promptInstance = Activator.CreateInstance(promptType) as IPrompts;
                        if (promptInstance == null)
                        {
                            LogWarning($"[Unity.Mcp] 无法将 {promptType.Name} 转换为IPrompts");
                            continue;
                        }

                        // 注册prompt
                        availablePrompts[promptInstance.Name] = promptInstance;

                        Log($"[Unity.Mcp] 成功注册Prompt: {promptInstance.Name} ({promptType.Name})");
                    }
                    catch (Exception ex)
                    {
                        LogError($"[Unity.Mcp] 创建Prompt实例失败 {promptType.Name}: {ex.Message}\n{ex.StackTrace}");
                    }
                }

                Log($"[Unity.Mcp] Prompts发现完成，共发现 {availablePrompts.Count} 个Prompts");

                // 列出所有注册的prompts
                foreach (var promptName in availablePrompts.Keys)
                {
                    Log($"[Unity.Mcp] 已注册Prompt: {promptName}");
                }
            }
            catch (Exception ex)
            {
                LogError($"[Unity.Mcp] Prompts发现过程中发生错误: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// 通过反射发现所有可用的Resources
        /// </summary>
        private void DiscoverResources()
        {
            Log("[Unity.Mcp] 开始发现Resources...");
            // 清空现有resources
            availableResources.Clear();

            try
            {
                // 获取所有程序集
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                Log($"[Unity.Mcp] 检查 {assemblies.Length} 个程序集中的Resources");

                // 查找所有实现IRes接口的类型
                var resourceTypes = assemblies
                    .SelectMany(assembly =>
                    {
                        try
                        {
                            return assembly.GetTypes();
                        }
                        catch (Exception ex)
                        {
                            LogWarning($"[Unity.Mcp] 无法获取程序集 {assembly.GetName().Name} 的类型: {ex.Message}");
                            return new Type[0];
                        }
                    })
                    .Where(type => !type.IsAbstract &&
                                   !type.IsInterface &&
                                   typeof(IRes).IsAssignableFrom(type))
                    .ToList();

                Log($"[Unity.Mcp] 找到 {resourceTypes.Count} 个实现IRes接口的类型");

                foreach (var resourceType in resourceTypes)
                {
                    try
                    {
                        Log($"[Unity.Mcp] 尝试创建Resource实例: {resourceType.FullName}");

                        // 创建resource实例
                        var resourceInstance = Activator.CreateInstance(resourceType) as IRes;
                        if (resourceInstance == null)
                        {
                            LogWarning($"[Unity.Mcp] 无法将 {resourceType.Name} 转换为IRes");
                            continue;
                        }

                        // 注册resource
                        availableResources[resourceInstance.Url] = resourceInstance;

                        Log($"[Unity.Mcp] 成功注册Resource: {resourceInstance.Url} ({resourceType.Name})");
                    }
                    catch (Exception ex)
                    {
                        LogError($"[Unity.Mcp] 创建Resource实例失败 {resourceType.Name}: {ex.Message}\n{ex.StackTrace}");
                    }
                }

                Log($"[Unity.Mcp] Resources发现完成，共发现 {availableResources.Count} 个Resources");

                // 列出所有注册的resources
                foreach (var resourceUrl in availableResources.Keys)
                {
                    Log($"[Unity.Mcp] 已注册Resource: {resourceUrl}");
                }
            }
            catch (Exception ex)
            {
                LogError($"[Unity.Mcp] Resources发现过程中发生错误: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// 添加AsyncCall工具到可用工具列表
        /// </summary>
        private void AddAsyncMcpTool()
        {
            try
            {
                string toolName = "async_call";
                Log($"[Unity.Mcp] 手动添加AsyncCall工具: {toolName}");

                // 将AsyncCall工具添加到缓存中，供GetMcpTool使用
                mcpToolInstanceCache[toolName] = new AsyncCall();

                // 简化工具信息和输入模式定义
                var toolInfo = new ToolInfo
                {
                    name = toolName,
                    description = "Unity异步调用工具，支持异步函数调用和结果获取",
                    inputSchema = new JsonClass
                    {
                        { "type", new JsonData("object") },
                        { "properties", new JsonClass
                            {
                                { "id", new JsonClass {
                                    { "type", new JsonData("string") },
                                    { "description", new JsonData("异步调用的唯一标识符") }
                                }},
                                { "type", new JsonClass {
                                    { "type", new JsonData("string") },
                                    { "description", new JsonData("操作类型: 'in' 开始调用, 'out' 获取结果") }
                                }},
                                { "func", new JsonClass {
                                    { "type", new JsonData("string") },
                                    { "description", new JsonData("要调用的函数名称（仅在type='in'时需要）") }
                                }},
                                { "args", new JsonClass {
                                    { "type", new JsonData("object") },
                                    { "description", new JsonData("函数参数（仅在type='in'时需要）") }
                                }},
                            }
                        },
                        { "required", new JsonArray {
                            new JsonData("id"),
                            new JsonData("type")
                        }}
                    }
                };
                toolInfos[toolName] = toolInfo;

                Log($"[Unity.Mcp] 成功添加AsyncCall工具: {toolName}");
            }
            catch (Exception ex)
            {
                LogError($"[Unity.Mcp] 添加AsyncCall工具失败: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// 添加BatchCall工具到可用工具列表
        /// </summary>
        private void AddBatchMcpTool()
        {
            try
            {
                string toolName = "batch_call";
                Log($"[Unity.Mcp] 手动添加BatchCall工具: {toolName}");

                // 将BatchCall工具添加到缓存中，供GetMcpTool使用
                mcpToolInstanceCache[toolName] = new BatchCall();

                // 创建工具信息并合并写法
                var toolInfo = new ToolInfo
                {
                    name = toolName,
                    description = "Unity批量调用工具，支持顺序执行多个函数调用",
                    inputSchema = new JsonClass {
                        { "type", new JsonData("object") },
                        { "properties", new JsonClass {
                            { "args", new JsonClass {
                                { "type", new JsonData("array") },
                                { "description", new JsonData("批量函数调用列表") },
                                { "items", new JsonClass {
                                    { "type", new JsonData("object") },
                                    { "properties", new JsonClass {
                                        { "func", new JsonClass {
                                            { "type", new JsonData("string") },
                                            { "description", new JsonData("要调用的函数名称") }
                                        }},
                                        { "args", new JsonClass {
                                            { "type", new JsonData("object") },
                                            { "description", new JsonData("函数参数") }
                                        }},
                                    }},
                                    { "required", new JsonArray {
                                        new JsonData("func"),
                                        new JsonData("args")
                                    }}
                                }}
                            }}
                        }},
                        { "required", new JsonArray {
                            new JsonData("args")
                        }}
                    }
                };
                toolInfos[toolName] = toolInfo;

                Log($"[Unity.Mcp] 成功添加BatchCall工具: {toolName}");
            }
            catch (Exception ex)
            {
                LogError($"[Unity.Mcp] 添加BatchCall工具失败: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// 获取工具名称，优先使用ToolNameAttribute
        /// </summary>
        private string GetToolName(Type toolType)
        {
            var toolNameAttribute = toolType.GetCustomAttribute<ToolNameAttribute>();
            if (toolNameAttribute != null)
            {
                return toolNameAttribute.ToolName;
            }

            // 转换类名为snake_case
            return ConvertToSnakeCase(toolType.Name);
        }

        /// <summary>
        /// 将Pascal命名法转换为snake_case命名法
        /// </summary>
        private string ConvertToSnakeCase(string pascalCase)
        {
            if (string.IsNullOrEmpty(pascalCase))
                return pascalCase;

            return System.Text.RegularExpressions.Regex.Replace(pascalCase, "(?<!^)([A-Z])", "_$1").ToLower();
        }

        /// <summary>
        /// 创建工具信息
        /// </summary>
        private ToolInfo CreateToolInfo(string toolName, IToolMethod toolInstance)
        {
            var toolInfo = new ToolInfo
            {
                name = toolName,
                description = $"Unity工具: {toolName}"
            };

            // 从Keys属性构建输入模式
            if (toolInstance.Keys != null && toolInstance.Keys.Length > 0)
            {
                var properties = new JsonClass();
                var required = new JsonArray();

                foreach (var key in toolInstance.Keys)
                {
                    var property = new JsonClass();

                    // 设置参数类型，默认为string
                    string paramType = !string.IsNullOrEmpty(key.Type) ? key.Type : "string";
                    property.Add("type", new JsonData(paramType));

                    // 设置描述
                    property.Add("description", new JsonData(key.Desc));

                    // 为所有数组类型添加items定义（通用处理）
                    if (paramType == "array" && !(key is MethodVector) && !(key is MethodArr))
                    {
                        var items = new JsonClass();
                        items.Add("type", new JsonData("string")); // 默认数组元素类型为string
                        property.Add("items", items);
                    }

                    // 处理数组类型的特殊属性
                    if (key is MethodArr methodArr)
                    {
                        var items = new JsonClass();
                        items.Add("type", new JsonData(methodArr.ItemType));
                        property.Add("items", items);
                    }

                    // 处理对象类型的特殊属性
                    if (key is MethodObj methodObj && methodObj.Properties.Count > 0)
                    {
                        var objProperties = new JsonClass();
                        foreach (var prop in methodObj.Properties)
                        {
                            var propDef = new JsonClass();
                            propDef.Add("type", new JsonData(prop.Value));

                            // 为数组类型添加items定义
                            if (prop.Value == "array")
                            {
                                var items = new JsonClass();
                                string itemType = "string"; // 默认类型

                                // 优先使用存储的itemType信息
                                if (methodObj.ArrayItemTypes.ContainsKey(prop.Key))
                                {
                                    itemType = methodObj.ArrayItemTypes[prop.Key];
                                }
                                // 如果没有存储的itemType，根据属性名推断数组元素类型
                                else if (prop.Key == "position" || prop.Key == "rotation" || prop.Key == "scale" ||
                                         prop.Key == "color" || prop.Key.Contains("vector") || prop.Key.Contains("Vector"))
                                {
                                    itemType = "number";
                                }

                                items.Add("type", new JsonData(itemType));
                                propDef.Add("items", items);
                            }

                            objProperties.Add(prop.Key, propDef);
                        }
                        property.Add("properties", objProperties);
                    }

                    // 处理向量类型的特殊属性
                    if (key is MethodVector methodVector)
                    {
                        // 强制设置类型为array，覆盖任何其他可能的类型
                        property["type"] = new JsonData("array");

                        // 为数组类型添加items定义
                        var items = new JsonClass();
                        items.Add("type", new JsonData("number"));
                        property.Add("items", items);

                        // 添加最小和最大长度限制
                        property.Add("minItems", new JsonData(methodVector.Dimension));
                        property.Add("maxItems", new JsonData(methodVector.Dimension));

                        // 添加严格的数组格式约束
                        property.Add("format", new JsonData($"vector{methodVector.Dimension}"));

                        // 明确说明只接受数组
                        property["description"] = new JsonData($"{key.Desc} [x, y, z]");
                    }

                    // 添加示例值
                    if (key.Examples != null && key.Examples.Count > 0)
                    {
                        var examplesArray = new JsonArray();
                        foreach (var example in key.Examples)
                        {
                            // 对于MethodVector，示例是字符串格式的数组，需要解析为JSON数组
                            if (key is MethodVector && example is string vectorStr && vectorStr.StartsWith("[") && vectorStr.EndsWith("]"))
                            {
                                try
                                {
                                    // 解析字符串格式的数组为JsonArray
                                    var vectorJson = Json.Parse(vectorStr);
                                    examplesArray.Add(vectorJson);
                                }
                                catch
                                {
                                    // 如果解析失败，作为字符串添加
                                    examplesArray.Add(new JsonData(example));
                                }
                            }
                            else
                            {
                                examplesArray.Add(new JsonData(example));
                            }
                        }
                        property.Add("examples", examplesArray);
                    }

                    // 添加枚举值
                    if (key.EnumValues != null && key.EnumValues.Count > 0)
                    {
                        var enumArray = new JsonArray();
                        foreach (var enumValue in key.EnumValues)
                        {
                            enumArray.Add(new JsonData(enumValue));
                        }
                        property.Add("enum", enumArray);
                    }

                    // 添加默认值
                    if (key.DefaultValue != null)
                    {
                        if (key.DefaultValue is string strValue)
                        {
                            property.Add("default", new JsonData(strValue));
                        }
                        else if (key.DefaultValue is int intValue)
                        {
                            property.Add("default", new JsonData(intValue));
                        }
                        else if (key.DefaultValue is bool boolValue)
                        {
                            property.Add("default", new JsonData(boolValue));
                        }
                        else if (key.DefaultValue is float floatValue)
                        {
                            property.Add("default", new JsonData(floatValue));
                        }
                        else if (key.DefaultValue is string[] strArrayValue)
                        {
                            var defaultArray = new JsonArray();
                            foreach (var item in strArrayValue)
                            {
                                defaultArray.Add(new JsonData(item));
                            }
                            property.Add("default", defaultArray);
                        }
                        else
                        {
                            property.Add("default", new JsonData(key.DefaultValue.ToString()));
                        }
                    }

                    properties.Add(key.Key, property);

                    if (!key.Optional)
                    {
                        required.Add(new JsonData(key.Key));
                    }
                }

                var schema = new JsonClass();
                schema.Add("type", new JsonData("object"));
                schema.Add("properties", properties);
                if (required.Count > 0)
                {
                    schema.Add("required", required);
                }

                toolInfo.inputSchema = schema;
            }

            return toolInfo;
        }

        private IEnumerator StartServiceDelay()
        {
            yield return new WaitForSeconds(1f);
            if (!isRunning)
                StartService();
        }
        /// <summary>
        /// 强制停止服务，确保资源完全释放
        /// </summary>
        private void ForceStop()
        {
            Log("[Unity.Mcp] 正在强制停止服务...");

            // 首先取消所有异步操作
            try
            {
                cancellationTokenSource?.Cancel();
                cancellationTokenSource?.Dispose();
                cancellationTokenSource = null;
            }
            catch (Exception ex)
            {
                LogError($"[Unity.Mcp] 取消异步操作时发生错误: {ex.Message}");
            }

            // 清理消息队列和注销update回调
            try
            {
                lock (queueLock)
                {
                    messageQueue.Clear();
                    if (isUpdateRegistered)
                    {
                        EditorApplication.update -= ProcessMessageQueue;
                        isUpdateRegistered = false;
                        Log("[Unity.Mcp] 消息队列处理器已强制注销");
                    }
                }
            }
            catch (Exception ex)
            {
                LogError($"[Unity.Mcp] 清理消息队列时发生错误: {ex.Message}");
            }

            if (isRunning)
            {
                Stop();
            }

            // 额外确保 HttpListener 被释放
            if (listener != null)
            {
                try
                {
                    // 先停止接受新请求
                    if (listener.IsListening)
                    {
                        listener.Stop();
                    }

                    // 等待一小段时间让正在处理的请求完成
                    Thread.Sleep(100);

                    // 强制关闭监听器
                    listener.Close();

                    // 释放资源
                    ((IDisposable)listener).Dispose();
                    listener = null;
                }
                catch (Exception ex)
                {
                    McpLogger.LogError($"[Unity.Mcp] 强制关闭 HttpListener 时发生错误: {ex.Message}");
                }
                McpLogger.Log("[Unity.Mcp] HttpListener 已强制关闭");
            }

            // 清空请求记录信息
            McpExecuteRecordObject.instance.ClearHttpRequestRecords();

            // 确保标记为已停止
            isRunning = false;

            // 强制垃圾回收，帮助释放网络资源
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        // 静态方法，方便外部调用
        public static void ForceStopService()
        {
            Instance.ForceStop();
        }

        /// <summary>
        /// 检查端口是否被占用
        /// </summary>
        private bool IsPortInUse(int port)
        {
            try
            {
                using (var client = new System.Net.Sockets.TcpClient())
                {
                    var result = client.BeginConnect("127.0.0.1", port, null, null);
                    var success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromMilliseconds(100));

                    if (success)
                    {
                        client.EndConnect(result);
                        Log($"[Unity.Mcp] 端口 {port} 已被占用");
                        return true;
                    }
                    else
                    {
                        Log($"[Unity.Mcp] 端口 {port} 可用");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"[Unity.Mcp] 端口 {port} 检测异常: {ex.Message}，假设可用");
                return false; // 假设端口可用
            }
        }

        /// <summary>
        /// 获取详细的端口状态信息
        /// </summary>
        public static string GetPortStatusInfo(int port)
        {
            try
            {
                var info = new System.Text.StringBuilder();
                info.AppendLine($"端口 {port} 状态检查:");

                // 检查端口是否被占用
                bool inUse = Instance.IsPortInUse(port);
                info.AppendLine($"- 端口占用状态: {(inUse ? "已占用" : "可用")}");

                // 检查防火墙设置（Windows）
                try
                {
                    var process = new System.Diagnostics.Process();
                    process.StartInfo.FileName = "netstat";
                    process.StartInfo.Arguments = $"-an | findstr :{port}";
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.CreateNoWindow = true;
                    process.Start();

                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();

                    if (!string.IsNullOrEmpty(output))
                    {
                        info.AppendLine($"- Netstat 输出:");
                        info.AppendLine(output);
                    }
                    else
                    {
                        info.AppendLine($"- Netstat: 端口 {port} 未在使用中");
                    }
                }
                catch (Exception ex)
                {
                    info.AppendLine($"- Netstat 检查失败: {ex.Message}");
                }

                return info.ToString();
            }
            catch (Exception ex)
            {
                return $"端口状态检查失败: {ex.Message}";
            }
        }

        // 静态方法，方便外部调用
        public static bool IsPortInUseStatic(int port)
        {
            return Instance.IsPortInUse(port);
        }

        // 实例方法
        public void Start()
        {
            McpLogger.Log($"[Unity.Mcp] <color=green>正在启动Unity MCP HTTP服务器...</color>");

            // 确保先停止现有服务
            Stop();

            // 等待一小段时间确保资源释放
            System.Threading.Thread.Sleep(200);

            if (isRunning)
            {
                McpLogger.Log($"[Unity.Mcp] 服务已在运行中");
                return;
            }

            // 初始化取消令牌
            cancellationTokenSource?.Cancel();
            cancellationTokenSource?.Dispose();
            cancellationTokenSource = new CancellationTokenSource();

            try
            {
                // 创建HttpListener监听MCP端口
                listener = new HttpListener();

                // 尝试不同的监听地址配置
                string[] prefixes = {
                    $"http://127.0.0.1:{mcpPort}/",
                    $"http://localhost:{mcpPort}/"
                };

                bool listenerStarted = false;
                string successPrefix = "";

                // 首先尝试基本配置
                foreach (string prefix in prefixes)
                {
                    try
                    {
                        listener = new HttpListener();
                        listener.Prefixes.Add(prefix);

                        // 配置HttpListener以提高稳定性
                        listener.IgnoreWriteExceptions = true; // 忽略写入异常，提高稳定性

                        listener.Start();
                        listenerStarted = true;
                        successPrefix = prefix;
                        Log($"[Unity.Mcp] 成功在 {prefix} 启动监听器");
                        McpLogger.Log($"[Unity.Mcp] HttpListener配置 - IgnoreWriteExceptions: {listener.IgnoreWriteExceptions}");
                        break;
                    }
                    catch (Exception ex)
                    {
                        Log($"[Unity.Mcp] 无法在 {prefix} 启动监听器: {ex.Message}");
                        listener?.Close();
                        listener = null;
                    }
                }

                // 如果基本配置失败，尝试管理员权限配置
                if (!listenerStarted)
                {
                    try
                    {
                        listener = new HttpListener();
                        string adminPrefix = $"http://+:{mcpPort}/";
                        listener.Prefixes.Add(adminPrefix);

                        // 配置HttpListener以提高稳定性
                        listener.IgnoreWriteExceptions = true; // 忽略写入异常，提高稳定性

                        listener.Start();
                        listenerStarted = true;
                        successPrefix = adminPrefix;
                        Log($"[Unity.Mcp] 成功在 {adminPrefix} 启动监听器（管理员模式）");
                        McpLogger.Log($"[Unity.Mcp] HttpListener配置（管理员模式） - IgnoreWriteExceptions: {listener.IgnoreWriteExceptions}");
                    }
                    catch (Exception ex)
                    {
                        LogError($"[Unity.Mcp] 管理员模式启动失败: {ex.Message}");
                        listener?.Close();
                        listener = null;
                    }
                }

                if (!listenerStarted)
                {
                    throw new Exception($"无法在端口 {mcpPort} 启动HTTP监听器。请检查端口是否被占用或需要管理员权限。");
                }

                // 在启动监听循环前缓存端口值，避免在后台线程访问EditorPrefs
                int port = mcpPort;
                // 启动监听循环并保存任务引用
                listenerTask = Task.Run(async () =>
                {
                    try
                    {
                        isRunning = true;
                        // 使用缓存的端口值而不是直接访问mcpPort属性
                        await McpListenerLoop(cancellationTokenSource.Token, port);
                    }
                    catch (Exception ex)
                    {
                        McpLogger.LogError($"[Unity.Mcp] 监听循环异常: {ex.Message}\n{ex.StackTrace}");
                        isRunning = false;
                    }
                });

                // 在启动服务时重新发现工具、prompts和resources，确保列表是最新的
                DiscoverTools();
                DiscoverPrompts();
                DiscoverResources();

                McpLogger.Log($"[Unity.Mcp] <color=green>MCP服务器成功启动!</color> 监听地址: {successPrefix}");
                McpLogger.Log($"[Unity.Mcp] 可用工具数量: {availableTools.Count}");

                // 打印所有可用工具的名称
                if (availableTools.Count > 0)
                {
                    McpLogger.Log($"[Unity.Mcp] 可用工具列表:");
                    foreach (var toolName in availableTools.Keys)
                    {
                        McpLogger.Log($"[Unity.Mcp] - {toolName}");
                    }
                }
                else
                {
                    McpLogger.LogWarning($"[Unity.Mcp] 没有发现可用工具，请检查IToolMethod接口的实现");
                }

                // 保存状态到 McpLocalSettings
                McpLocalSettings.Instance.McpOpenState = true;

                Log($"[Unity.Mcp] MCP服务器启动完成");
            }
            catch (Exception ex)
            {
                LogError($"[Unity.Mcp] 启动MCP服务器失败: {ex.Message}");
                LogError($"[Unity.Mcp] 错误堆栈: {ex.StackTrace}");

                if (ex.Message.Contains("Access is denied"))
                {
                    LogError("[Unity.Mcp] 请以管理员权限运行Unity编辑器，或者更改端口号");
                }
                else if (ex.Message.Contains("port") || ex.Message.Contains("端口"))
                {
                    LogError($"[Unity.Mcp] 端口 {mcpPort} 可能被占用，请尝试更改端口号");
                    LogError($"[Unity.Mcp] 当前端口状态: {GetPortStatusInfo(mcpPort)}");
                }

                // 确保服务状态正确
                isRunning = false;
                listener = null;
                McpLocalSettings.Instance.McpOpenState = false;
            }
        }

        // 静态方法，方便外部调用
        public static void StartService()
        {
            Instance.Start();
        }

        // 实例方法
        public void Stop()
        {
            if (!isRunning && listener == null)
            {
                return;
            }

            McpLogger.Log($"[Unity.Mcp] <color=orange>正在停止Unity MCP HTTP服务器...</color>");

            try
            {
                // 首先取消所有异步操作
                try
                {
                    cancellationTokenSource?.Cancel();
                }
                catch (Exception ex)
                {
                    LogError($"[Unity.Mcp] 取消异步操作时发生错误: {ex.Message}");
                }

                // 清空请求记录信息
                McpExecuteRecordObject.instance.ClearHttpRequestRecords();

                // 关闭和释放 HttpListener
                if (listener != null)
                {
                    try
                    {
                        // 先停止接受新请求
                        if (listener.IsListening)
                        {
                            listener.Stop();
                        }

                        // 等待一小段时间让正在处理的请求完成
                        Thread.Sleep(50);

                        // 关闭监听器
                        listener.Close();

                        // 释放资源
                        ((IDisposable)listener).Dispose();
                        listener = null;
                    }
                    catch (Exception listenerEx)
                    {
                        McpLogger.LogError($"[Unity.Mcp] 关闭 HttpListener 时发生错误: {listenerEx.Message}");
                    }

                    McpLogger.Log("[Unity.Mcp] MCP HttpListener 已关闭");
                }

                // 等待监听任务完成
                try
                {
                    if (listenerTask != null && !listenerTask.IsCompleted)
                    {
                        // 给任务一个短暂的时间完成
                        var timeoutTask = Task.Delay(500);
                        Task.WaitAny(new[] { listenerTask, timeoutTask });

                        if (!listenerTask.IsCompleted)
                        {
                            Log($"[Unity.Mcp] 监听任务未能在超时时间内完成");
                        }
                    }
                    listenerTask = null;
                }
                catch (Exception ex)
                {
                    LogError($"[Unity.Mcp] 等待监听任务完成时发生错误: {ex.Message}");
                }

                // 清理取消令牌
                try
                {
                    cancellationTokenSource?.Dispose();
                    cancellationTokenSource = null;
                }
                catch (Exception ex)
                {
                    LogError($"[Unity.Mcp] 清理取消令牌时发生错误: {ex.Message}");
                }

                // 标记状态
                isRunning = false;

                // 保存状态到 McpLocalSettings
                McpLocalSettings.Instance.McpOpenState = false;

                McpLogger.Log($"[Unity.Mcp] <color=orange>MCP服务已停止</color>");

                // 等待一小段时间确保资源释放
                System.Threading.Thread.Sleep(100);
            }
            catch (Exception ex)
            {
                McpLogger.LogError($"[Unity.Mcp] 停止服务时发生错误: {ex.Message}");

                // 确保状态正确
                isRunning = false;
                listener = null;
            }
        }

        // 静态方法，方便外部调用
        public static void StopService()
        {
            Instance.Stop();
        }

        /// <summary>
        /// MCP监听循环
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <param name="port">监听端口（在主线程中预先缓存的值）</param>
        private async Task McpListenerLoop(CancellationToken cancellationToken, int port)
        {
            Log($"[Unity.Mcp] MCP监听循环已启动，端口: {port}");
            Log($"[Unity.Mcp] 监听器状态 - IsListening: {listener?.IsListening}, IsRunning: {isRunning}");

            int requestCount = 0;
            while (isRunning && listener != null && listener.IsListening && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    if (requestCount == 0)
                    {
                        Log($"[Unity.Mcp] 开始等待MCP请求...");
                    }

                    // 使用带取消令牌的GetContextAsync
                    var context = await listener.GetContextAsync();
                    requestCount++;

                    Log($"[Unity.Mcp] 收到第 {requestCount} 个请求");

                    // Fire and forget each HTTP request with cancellation token
                    // 传递端口参数，避免在后台线程访问EditorPrefs
                    _ = HandleMcpRequestAsync(context, cancellationToken, port);
                }
                catch (HttpListenerException ex)
                {
                    if (isRunning && !cancellationToken.IsCancellationRequested)
                    {
                        LogError($"[Unity.Mcp] MCP监听器错误 (HttpListenerException): {ex.Message} (ErrorCode: {ex.ErrorCode})");
                    }
                    else
                    {
                        Log($"[Unity.Mcp] MCP监听器已停止 (HttpListenerException)");
                    }
                    break; // Exit loop if listener is closed or has an error
                }
                catch (ObjectDisposedException ex)
                {
                    Log($"[Unity.Mcp] MCP监听器已被释放，循环终止: {ex.Message}");
                    break; // Exit loop if listener is disposed
                }
                catch (OperationCanceledException)
                {
                    Log($"[Unity.Mcp] MCP监听循环已被取消");
                    break; // Exit loop if operation is cancelled
                }
                catch (Exception ex)
                {
                    if (isRunning && !cancellationToken.IsCancellationRequested)
                    {
                        LogError($"[Unity.Mcp] MCP监听器未知错误: {ex.GetType().Name} - {ex.Message}");
                        LogError($"[Unity.Mcp] 堆栈跟踪: {ex.StackTrace}");
                    }
                }
            }

            Log($"[Unity.Mcp] MCP监听循环已结束 (总共处理了 {requestCount} 个请求)");
            Log($"[Unity.Mcp] 结束状态 - IsRunning: {isRunning}, IsListening: {listener?.IsListening}, IsCancelled: {cancellationToken.IsCancellationRequested}");
        }

        /// <summary>
        /// 处理 MCP HTTP 请求
        /// </summary>
        /// <param name="context">HTTP监听上下文</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <param name="port">监听端口（在主线程中预先缓存的值）</param>
        private async Task HandleMcpRequestAsync(HttpListenerContext context, CancellationToken cancellationToken, int port)
        {
            string clientEndpoint = context.Request.RemoteEndPoint?.ToString() ?? "Unknown";
            string clientId = Guid.NewGuid().ToString();
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;

            // 增强请求日志 - 记录更多详细信息
            McpLogger.Log($"[Unity.Mcp] <color=cyan>收到MCP请求:</color> {request.HttpMethod} {request.Url} from {clientEndpoint} (ID: {clientId})");
            McpLogger.Log($"[Unity.Mcp] 请求头信息:");
            McpLogger.Log($"[Unity.Mcp] - Content-Type: {request.ContentType}");
            McpLogger.Log($"[Unity.Mcp] - Accept: {request.Headers["Accept"]}");
            McpLogger.Log($"[Unity.Mcp] - User-Agent: {request.Headers["User-Agent"]}");
            McpLogger.Log($"[Unity.Mcp] - Content-Length: {request.ContentLength64}");

            try
            {
                // 设置响应头，允许跨域
                response.Headers.Add("Access-Control-Allow-Origin", "*");
                response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
                response.Headers.Add("Access-Control-Allow-Headers", "Content-Type");

                // 检查是否是SSE请求 - 只有GET请求才可能是SSE
                bool isSSERequest = false;

                // 检查Accept头
                string acceptHeader = request.Headers["Accept"];
                McpLogger.Log($"[Unity.Mcp] 请求方法: {request.HttpMethod}, Accept头: {acceptHeader}");

                // 只有GET请求才检查SSE
                if (request.HttpMethod == "GET")
                {
                    if (acceptHeader != null && acceptHeader.Contains("text/event-stream"))
                    {
                        isSSERequest = true;
                        McpLogger.Log($"[Unity.Mcp] 通过Accept头检测到SSE请求");
                    }

                    // 检查URL路径，有些客户端通过路径请求SSE
                    string path = request.Url.AbsolutePath.ToLowerInvariant();
                    McpLogger.Log($"[Unity.Mcp] 请求路径: {path}");

                    if (path.EndsWith("/sse") || path.Contains("/events") || request.QueryString["stream"] == "true")
                    {
                        isSSERequest = true;
                        McpLogger.Log($"[Unity.Mcp] 通过路径检测到SSE请求");
                    }
                }

                McpLogger.Log($"[Unity.Mcp] SSE请求检测结果: {isSSERequest}");

                try
                {
                    if (isSSERequest)
                    {
                        McpLogger.Log($"[Unity.Mcp] 检测到SSE请求，返回不支持SSE的响应");

                        // 对于SSE请求，返回一个明确的错误响应，告知客户端使用HTTP POST
                        response.StatusCode = 501; // Not Implemented
                        response.ContentType = "application/json";

                        string errorResponse = CreateMcpErrorResponse(null, -32601, "SSE not supported, please use HTTP POST for MCP requests");
                        byte[] errorBytes = Encoding.UTF8.GetBytes(errorResponse);

                        try
                        {
                            await response.OutputStream.WriteAsync(errorBytes, 0, errorBytes.Length);
                            await response.OutputStream.FlushAsync();
                            response.Close();

                            McpLogger.Log($"[Unity.Mcp] SSE不支持响应已发送");
                        }
                        catch (Exception sseEx)
                        {
                            McpLogger.LogError($"[Unity.Mcp] 发送SSE不支持响应失败: {sseEx.Message}");
                            try { response.Close(); } catch { }
                        }

                        return;
                    }
                    else
                    {
                        // 默认JSON内容类型
                        response.ContentType = "application/json";
                    }
                }
                catch (Exception ex)
                {
                    LogError($"[Unity.Mcp] 设置响应头时出错: {ex.Message}");
                    // 如果设置头部失败，确保使用默认的JSON内容类型
                    response.ContentType = "application/json";
                }

                response.ContentEncoding = Encoding.UTF8;

                // 检查是否已被取消
                cancellationToken.ThrowIfCancellationRequested();

                // 处理 OPTIONS 预检请求
                if (request.HttpMethod == "OPTIONS")
                {
                    response.StatusCode = 200;
                    response.Close();
                    Log($"[Unity.Mcp] OPTIONS预检请求处理完成 from {clientEndpoint}");
                    return;
                }

                // 处理GET请求 - 返回服务器状态信息
                if (request.HttpMethod == "GET")
                {
                    try
                    {
                        var serverInfo = new JsonClass();
                        serverInfo.Add("name", new JsonData(serverName));
                        serverInfo.Add("version", new JsonData(serverVersion));
                        serverInfo.Add("status", new JsonData("running"));
                        serverInfo.Add("port", new JsonData(port)); // 使用传入的端口值，而不是mcpPort属性
                        serverInfo.Add("toolCount", new JsonData(toolInfos.Count));
                        serverInfo.Add("protocol", new JsonData("MCP"));
                        serverInfo.Add("protocolVersion", new JsonData("2024-11-05"));

                        string getResponseJson = serverInfo.ToString();
                        byte[] getResponseBytes = Encoding.UTF8.GetBytes(getResponseJson);

                        // 设置状态码 - HttpListenerResponse没有HeadersSent属性，
                        // 我们需要在try-catch块中设置状态码
                        try
                        {
                            response.StatusCode = 200;
                        }
                        catch (InvalidOperationException)
                        {
                            // 如果头部已经发送，会抛出InvalidOperationException
                            McpLogger.Log("[Unity.Mcp] 响应头已发送，无法设置状态码");
                        }

                        await response.OutputStream.WriteAsync(getResponseBytes, 0, getResponseBytes.Length);
                        response.Close();
                        Log($"[Unity.Mcp] GET请求处理完成 from {clientEndpoint}");
                    }
                    catch (Exception ex)
                    {
                        LogError($"[Unity.Mcp] 处理GET请求时出错: {ex.Message}");

                        // 尝试设置错误状态码
                        try
                        {
                            response.StatusCode = 500;
                        }
                        catch (InvalidOperationException)
                        {
                            // 如果头部已经发送，会抛出InvalidOperationException
                            McpLogger.Log("[Unity.Mcp] 响应头已发送，无法设置错误状态码");
                        }

                        try
                        {
                            response.Close();
                        }
                        catch { }
                    }

                    // GET请求不需要记录到请求记录中，直接返回
                    return;
                }

                // 只接受 POST 和 GET 请求
                if (request.HttpMethod != "POST")
                {
                    response.StatusCode = 405; // Method Not Allowed
                    byte[] errorBytes = Encoding.UTF8.GetBytes(
                        /*lang=json,strict*/
                        "{\"jsonrpc\":\"2.0\",\"error\":{\"code\":-32601,\"message\":\"Method not allowed\"},\"id\":null}"
                    );
                    await response.OutputStream.WriteAsync(errorBytes, 0, errorBytes.Length);
                    response.Close();
                    LogWarning($"[Unity.Mcp] 不支持的HTTP方法: {request.HttpMethod} from {clientEndpoint}");
                    return;
                }

                // 读取请求体
                string requestBody = "";
                try
                {
                    using (StreamReader reader = new StreamReader(request.InputStream, request.ContentEncoding))
                    {
                        requestBody = await reader.ReadToEndAsync();
                    }

                    Log($"[Unity.Mcp] 接收到MCP请求 from {clientEndpoint}: {requestBody}");

                    // 验证请求体不为空
                    if (string.IsNullOrWhiteSpace(requestBody))
                    {
                        McpLogger.LogWarning($"[Unity.Mcp] 收到空的请求体 from {clientEndpoint}");

                        // 发送错误响应
                        string errorResponse = CreateMcpErrorResponse(null, -32600, "Empty request body");
                        byte[] errorBytes = Encoding.UTF8.GetBytes(errorResponse);

                        try
                        {
                            response.StatusCode = 400;
                        }
                        catch (InvalidOperationException) { }

                        await response.OutputStream.WriteAsync(errorBytes, 0, errorBytes.Length);
                        response.Close();
                        return;
                    }
                }
                catch (Exception ex)
                {
                    LogError($"[Unity.Mcp] 读取请求体时出错 from {clientEndpoint}: {ex.Message}");

                    // 发送错误响应
                    string errorResponse = CreateMcpErrorResponse(null, -32700, "Failed to read request body");
                    byte[] errorBytes = Encoding.UTF8.GetBytes(errorResponse);

                    try
                    {
                        response.StatusCode = 400;
                    }
                    catch (InvalidOperationException) { }

                    await response.OutputStream.WriteAsync(errorBytes, 0, errorBytes.Length);
                    response.Close();
                    return;
                }

                // 记录请求信息到McpExecuteRecordObject
                McpExecuteRecordObject.instance.AddHttpRequestRecord(
                    clientId,
                    clientEndpoint,
                    DateTime.Now,
                    requestBody,
                    request.HttpMethod
                );

                // 处理MCP请求
                McpLogger.Log($"[Unity.Mcp] 开始处理MCP请求 from {clientEndpoint}");
                string responseJson = await ProcessMcpRequest(requestBody);
                McpLogger.Log($"[Unity.Mcp] MCP请求处理完成，准备发送响应 to {clientEndpoint}");

                byte[] responseBytes = Encoding.UTF8.GetBytes(responseJson);

                // 尝试设置状态码
                try
                {
                    response.StatusCode = 200;
                    McpLogger.Log($"[Unity.Mcp] 设置响应状态码为200");
                }
                catch (InvalidOperationException)
                {
                    // 如果头部已经发送，会抛出InvalidOperationException
                    McpLogger.Log("[Unity.Mcp] 响应头已发送，无法设置状态码");
                }

                try
                {
                    McpLogger.Log($"[Unity.Mcp] 开始写入响应数据，长度: {responseBytes.Length} bytes");
                    await response.OutputStream.WriteAsync(responseBytes, 0, responseBytes.Length);
                    await response.OutputStream.FlushAsync();
                    McpLogger.Log($"[Unity.Mcp] 响应数据写入完成");

                    response.Close();
                    McpLogger.Log($"[Unity.Mcp] 响应连接已关闭");
                }
                catch (Exception ex)
                {
                    LogError($"[Unity.Mcp] 发送响应时出错 to {clientEndpoint}: {ex.Message}");
                    throw; // 重新抛出异常以便外层catch处理
                }

                Log($"[Unity.Mcp] MCP响应已发送 to {clientEndpoint}");

                // 更新请求记录的完成时间
                McpExecuteRecordObject.instance.UpdateHttpRequestRecord(
                    clientId,
                    responseJson,
                    true,
                    200,
                    DateTime.Now
                );
            }
            catch (Exception ex)
            {
                LogError($"[Unity.Mcp] MCP请求处理异常 {clientEndpoint}: {ex.Message}");
                LogError($"[Unity.Mcp] 异常堆栈: {ex.StackTrace}");

                try
                {
                    McpLogger.Log($"[Unity.Mcp] 检查响应流状态，CanWrite: {response.OutputStream.CanWrite}");

                    if (!response.OutputStream.CanWrite)
                    {
                        McpLogger.LogWarning($"[Unity.Mcp] 响应流不可写，跳过错误响应发送");
                        return;
                    }

                    // 尝试设置错误状态码
                    try
                    {
                        response.StatusCode = 500;
                        McpLogger.Log($"[Unity.Mcp] 设置错误状态码为500");
                    }
                    catch (InvalidOperationException)
                    {
                        // 如果头部已经发送，会抛出InvalidOperationException
                        McpLogger.Log("[Unity.Mcp] 响应头已发送，无法设置错误状态码");
                    }

                    string errorMessage = ex.Message.Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
                    string errorResponse = $"{{\"jsonrpc\":\"2.0\",\"error\":{{\"code\":-32603,\"message\":\"{errorMessage}\"}},\"id\":null}}";
                    byte[] errorBytes = Encoding.UTF8.GetBytes(errorResponse);

                    McpLogger.Log($"[Unity.Mcp] 准备发送错误响应: {errorResponse}");
                    McpLogger.Log($"[Unity.Mcp] 错误响应字节长度: {errorBytes.Length}");

                    await response.OutputStream.WriteAsync(errorBytes, 0, errorBytes.Length);
                    await response.OutputStream.FlushAsync();
                    McpLogger.Log($"[Unity.Mcp] 错误响应数据写入并刷新完成");

                    response.Close();
                    McpLogger.Log($"[Unity.Mcp] 错误响应连接已关闭");
                }
                catch (Exception innerEx)
                {
                    // 无法发送错误响应
                    LogError($"[Unity.Mcp] 无法发送错误响应: {innerEx.Message}");
                    LogError($"[Unity.Mcp] 内部异常堆栈: {innerEx.StackTrace}");

                    try
                    {
                        response.StatusCode = 500;
                        response.Close();
                        McpLogger.Log($"[Unity.Mcp] 强制关闭响应连接");
                    }
                    catch (InvalidOperationException)
                    {
                        // 如果头部已经发送，会抛出InvalidOperationException
                        McpLogger.Log("[Unity.Mcp] 响应头已发送，无法设置错误状态码");
                    }
                    catch (Exception finalEx)
                    {
                        // 忽略其他异常
                        LogError($"[Unity.Mcp] 最终异常处理失败: {finalEx.Message}");
                    }
                }

                // 更新请求记录的完成时间（即使出错也要记录）
                McpExecuteRecordObject.instance.UpdateHttpRequestRecord(
                    clientId,
                    ex.Message,
                    false,
                    500,
                    DateTime.Now
                );
            }
        }

        /// <summary>
        /// 处理MCP请求
        /// </summary>
        private async Task<string> ProcessMcpRequest(string requestBody)
        {
            try
            {
                // 添加处理时间日志
                Stopwatch sw = new Stopwatch();
                sw.Start();

                McpLogger.Log($"[Unity.Mcp] 开始处理MCP请求，请求体长度: {requestBody?.Length ?? 0}");

                // 解析JSON-RPC请求
                if (string.IsNullOrWhiteSpace(requestBody))
                {
                    McpLogger.LogError($"[Unity.Mcp] 请求体为空或null");
                    return CreateMcpErrorResponse(null, -32600, "Invalid Request");
                }

                McpLogger.Log($"[Unity.Mcp] 请求体内容: {requestBody}");

                JsonNode requestJson;
                try
                {
                    requestJson = Json.Parse(requestBody);
                }
                catch (Exception parseEx)
                {
                    McpLogger.LogError($"[Unity.Mcp] JSON解析失败: {parseEx.Message}");
                    return CreateMcpErrorResponse(null, -32700, $"Parse error: {parseEx.Message}");
                }

                if (requestJson == null)
                {
                    McpLogger.LogError($"[Unity.Mcp] JSON解析结果为null");
                    return CreateMcpErrorResponse(null, -32700, "Parse error");
                }

                var request = requestJson.ToObject();
                string method = request["method"]?.Value;
                string id = request["id"]?.Value;
                JsonNode paramsNode = request["params"];

                McpLogger.Log($"[Unity.Mcp] 解析成功 - 方法: {method}, ID: {id}");

                // 设置超时保护
                CancellationTokenSource timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

                try
                {
                    // 根据方法类型处理请求
                    string result;
                    switch (method)
                    {
                        case "initialize":
                            result = HandleInitialize(id, paramsNode);
                            break;

                        case "notifications/initialized":
                            result = HandleInitializedNotification(id);
                            break;

                        case "tools/list":
                            result = HandleToolsList(id);
                            break;

                        case "tools/call":
                            McpLogger.Log($"[Unity.Mcp] 收到工具调用请求，ID: {id}");

                            // 检查工具是否已发现
                            if (toolInfos.Count == 0)
                            {
                                McpLogger.LogWarning($"[Unity.Mcp] 工具列表为空，重新发现工具...");
                                DiscoverTools();
                            }

                            // 使用超时保护
                            Task<string> callTask = HandleToolsCall(id, paramsNode);

                            // 等待任务完成或超时
                            if (await Task.WhenAny(callTask, Task.Delay(10000)) == callTask)
                            {
                                // 任务完成
                                result = await callTask;
                                McpLogger.Log($"[Unity.Mcp] 工具调用完成，ID: {id}");
                            }
                            else
                            {
                                // 任务超时
                                McpLogger.LogError($"[Unity.Mcp] 工具调用超时，ID: {id}");
                                return CreateMcpErrorResponse(id, -32000, "Tool call timed out after 10 seconds");
                            }
                            break;

                        case "prompts/list":
                            result = HandlePromptsList(id);
                            break;

                        case "prompts/get":
                            result = HandlePromptsGet(id, paramsNode);
                            break;

                        case "resources/list":
                            result = HandleResourcesList(id);
                            break;

                        case "resources/read":
                            result = HandleResourcesRead(id, paramsNode);
                            break;

                        default:
                            result = CreateMcpErrorResponse(id, -32601, $"Method not found: {method}");
                            break;
                    }

                    // 记录处理时间
                    sw.Stop();
                    Log($"[Unity.Mcp] 方法 {method} 处理完成，耗时: {sw.ElapsedMilliseconds}ms");

                    return result;
                }
                catch (OperationCanceledException)
                {
                    // 处理超时
                    return CreateMcpErrorResponse(id, -32000, "Request processing timed out");
                }
                finally
                {
                    timeoutCts.Dispose();
                }
            }
            catch (Exception ex)
            {
                LogError($"[Unity.Mcp] 处理MCP请求时发生错误: {ex.Message}");
                return CreateMcpErrorResponse(null, -32603, $"Internal error: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理notifications/initialized通知
        /// </summary>
        private string HandleInitializedNotification(string id)
        {
            Log("[Unity.Mcp] 处理notifications/initialized通知");

            // 对于通知类型的请求，通常不需要返回响应
            // 但是为了保持一致性，我们返回一个简单的成功响应
            var result = new JsonClass();
            result.Add("status", new JsonData("initialized"));
            result.Add("message", new JsonData("Server initialized successfully"));

            Log("[Unity.Mcp] notifications/initialized通知处理完成");
            return CreateMcpSuccessResponse(id, result);
        }

        /// <summary>
        /// 处理initialize请求
        /// </summary>
        private string HandleInitialize(string id, JsonNode paramsNode)
        {
            Log("[Unity.Mcp] 处理initialize请求");

            // 详细记录初始化参数
            if (paramsNode != null)
            {
                Log($"[Unity.Mcp] 初始化参数: {paramsNode}");
            }

            // 如果工具列表为空，尝试重新发现工具
            if (toolInfos.Count == 0)
            {
                McpLogger.LogWarning("[Unity.Mcp] 初始化时工具列表为空，尝试重新发现工具...");
                DiscoverTools();
                McpLogger.Log($"[Unity.Mcp] 重新发现后的工具数量: {toolInfos.Count}");
            }

            var result = new JsonClass();
            result.Add("protocolVersion", new JsonData("2024-11-05"));

            var capabilities = new JsonClass();

            // Tools capability
            var toolsCapability = new JsonClass();
            // 动态检查工具数量是否发生变化
            bool hasChanged = HasToolCountChanged();
            toolsCapability.Add("listChanged", new JsonData(hasChanged));
            Log($"[Unity.Mcp] 初始化响应 - listChanged: {hasChanged}");

            // 如果工具数量发生了变化，更新保存的数量以便下次比较
            if (hasChanged)
            {
                McpLocalSettings.Instance.LastToolCount = availableTools.Count;
                Log($"[Unity.Mcp] 工具数量已变化，更新保存的数量: {availableTools.Count}");
            }
            capabilities.Add("tools", toolsCapability);

            // Prompts capability
            var promptsCapability = new JsonClass();
            promptsCapability.Add("listChanged", new JsonData(true)); // 暂时总是返回true
            capabilities.Add("prompts", promptsCapability);

            // Resources capability  
            var resourcesCapability = new JsonClass();
            resourcesCapability.Add("listChanged", new JsonData(true)); // 暂时总是返回true
            resourcesCapability.Add("subscribe", new JsonData(false)); // 暂不支持订阅
            capabilities.Add("resources", resourcesCapability);
            result.Add("capabilities", capabilities);

            var serverInfo = new JsonClass();
            serverInfo.Add("name", new JsonData(serverName));
            serverInfo.Add("version", new JsonData(serverVersion));
            result.Add("serverInfo", serverInfo);

            // 直接在初始化响应中包含启用的工具列表
            var enabledToolNames = GetEnabledToolNames();
            var toolsArray = new JsonArray();
            foreach (var toolInfo in toolInfos.Values)
            {
                // 只添加启用的工具
                if (enabledToolNames.Contains(toolInfo.name))
                {
                    Log($"[Unity.Mcp] 在初始化响应中添加启用的工具: {toolInfo.name}");
                    var tool = new JsonClass();
                    tool.Add("name", new JsonData(toolInfo.name));
                    tool.Add("description", new JsonData(toolInfo.description));
                    if (toolInfo.inputSchema != null)
                    {
                        tool.Add("inputSchema", toolInfo.inputSchema);
                    }
                    toolsArray.Add(tool);
                }
                else
                {
                    Log($"[Unity.Mcp] 在初始化响应中跳过禁用的工具: {toolInfo.name}");
                }
            }
            string responseJson = CreateMcpSuccessResponse(id, result);
            McpLogger.Log($"[Unity.Mcp] 初始化响应包含 {toolsArray.Count} 个启用的工具");
            return responseJson;
        }

        /// <summary>
        /// 处理tools/list请求
        /// </summary>
        private string HandleToolsList(string id)
        {
            Log($"[Unity.Mcp] 处理tools/list请求，当前工具数量: {toolInfos.Count}");

            // 如果工具列表为空，尝试重新发现工具
            if (toolInfos.Count == 0)
            {
                McpLogger.LogWarning("[Unity.Mcp] 工具列表为空，尝试重新发现工具...");
                DiscoverTools();
                McpLogger.Log($"[Unity.Mcp] 重新发现后的工具数量: {toolInfos.Count}");
            }

            // 获取启用的工具名称列表
            var enabledToolNames = GetEnabledToolNames();
            Log($"[Unity.Mcp] 启用的工具数量: {enabledToolNames.Count}");

            var tools = new JsonArray();
            foreach (var toolInfo in toolInfos.Values)
            {
                // 只添加启用的工具
                if (enabledToolNames.Contains(toolInfo.name))
                {
                    Log($"[Unity.Mcp] 添加启用的工具到列表: {toolInfo.name}");
                    var tool = new JsonClass();
                    tool.Add("name", new JsonData(toolInfo.name));
                    tool.Add("description", new JsonData(toolInfo.description));
                    if (toolInfo.inputSchema != null)
                    {
                        tool.Add("inputSchema", toolInfo.inputSchema);
                    }
                    tools.Add(tool);
                }
                else
                {
                    Log($"[Unity.Mcp] 跳过禁用的工具: {toolInfo.name}");
                }
            }

            var result = new JsonClass();
            result.Add("tools", tools);

            string responseJson = CreateMcpSuccessResponse(id, result);
            Log($"[Unity.Mcp] tools/list响应: {responseJson}");
            McpLogger.Log($"[Unity.Mcp] 返回启用的工具数量: {tools.Count}");
            return responseJson;
        }

        /// <summary>
        /// 处理prompts/list请求
        /// </summary>
        private string HandlePromptsList(string id)
        {
            Log($"[Unity.Mcp] 处理prompts/list请求，当前Prompts数量: {availablePrompts.Count}");

            // 如果prompts列表为空，尝试重新发现
            if (availablePrompts.Count == 0)
            {
                McpLogger.LogWarning("[Unity.Mcp] Prompts列表为空，尝试重新发现Prompts...");
                DiscoverPrompts();
                McpLogger.Log($"[Unity.Mcp] 重新发现后的Prompts数量: {availablePrompts.Count}");
            }

            var prompts = new JsonArray();
            foreach (var prompt in availablePrompts.Values)
            {
                Log($"[Unity.Mcp] 添加Prompt到列表: {prompt.Name}");
                var promptObj = new JsonClass();
                promptObj.Add("name", new JsonData(prompt.Name));
                promptObj.Add("description", new JsonData(prompt.Description));

                // 添加参数信息
                if (prompt.Keys != null && prompt.Keys.Length > 0)
                {
                    var arguments = new JsonArray();
                    foreach (var key in prompt.Keys)
                    {
                        var arg = new JsonClass();
                        arg.Add("name", new JsonData(key.Key));
                        arg.Add("description", new JsonData(key.Desc));
                        arg.Add("required", new JsonData(!key.Optional));
                        arguments.Add(arg);
                    }
                    promptObj.Add("arguments", arguments);
                }

                prompts.Add(promptObj);
            }

            var result = new JsonClass();
            result.Add("prompts", prompts);

            string responseJson = CreateMcpSuccessResponse(id, result);
            Log($"[Unity.Mcp] prompts/list响应: {responseJson}");
            McpLogger.Log($"[Unity.Mcp] 返回Prompts数量: {prompts.Count}");
            return responseJson;
        }

        /// <summary>
        /// 处理prompts/get请求
        /// </summary>
        private string HandlePromptsGet(string id, JsonNode paramsNode)
        {
            try
            {
                if (paramsNode == null)
                {
                    return CreateMcpErrorResponse(id, -32602, "Invalid params");
                }

                var paramsObj = paramsNode.ToObject();
                string promptName = paramsObj["name"]?.Value;

                if (string.IsNullOrEmpty(promptName))
                {
                    return CreateMcpErrorResponse(id, -32602, "Prompt name is required");
                }

                Log($"[Unity.Mcp] 处理prompts/get请求，Prompt名: {promptName}");

                if (!availablePrompts.TryGetValue(promptName, out var prompt))
                {
                    return CreateMcpErrorResponse(id, -32602, $"Prompt not found: {promptName}");
                }

                var result = new JsonClass();
                result.Add("description", new JsonData(prompt.Description));

                // 构建消息数组
                var messages = new JsonArray();
                var message = new JsonClass();
                message.Add("role", new JsonData("user"));

                var content = new JsonClass();
                content.Add("type", new JsonData("text"));
                content.Add("text", new JsonData($"This is the {prompt.Name} prompt: {prompt.Description}"));

                var contentArray = new JsonArray();
                contentArray.Add(content);
                message.Add("content", contentArray);
                messages.Add(message);

                result.Add("messages", messages);

                return CreateMcpSuccessResponse(id, result);
            }
            catch (Exception ex)
            {
                LogError($"[Unity.Mcp] 处理prompts/get请求失败: {ex.Message}");
                return CreateMcpErrorResponse(id, -32603, $"Internal error: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理resources/list请求
        /// </summary>
        private string HandleResourcesList(string id)
        {
            Log($"[Unity.Mcp] 处理resources/list请求，当前Resources数量: {availableResources.Count}");

            // 如果resources列表为空，尝试重新发现
            if (availableResources.Count == 0)
            {
                McpLogger.LogWarning("[Unity.Mcp] Resources列表为空，尝试重新发现Resources...");
                DiscoverResources();
                McpLogger.Log($"[Unity.Mcp] 重新发现后的Resources数量: {availableResources.Count}");
            }

            var resources = new JsonArray();
            foreach (var resource in availableResources.Values)
            {
                Log($"[Unity.Mcp] 添加Resource到列表: {resource.Url}");
                var resourceObj = new JsonClass();
                resourceObj.Add("uri", new JsonData(resource.Url));
                resourceObj.Add("name", new JsonData(resource.Name));
                resourceObj.Add("description", new JsonData(resource.Description));
                resourceObj.Add("mimeType", new JsonData(resource.MimeType));
                resources.Add(resourceObj);
            }

            var result = new JsonClass();
            result.Add("resources", resources);

            string responseJson = CreateMcpSuccessResponse(id, result);
            Log($"[Unity.Mcp] resources/list响应: {responseJson}");
            McpLogger.Log($"[Unity.Mcp] 返回Resources数量: {resources.Count}");
            return responseJson;
        }

        /// <summary>
        /// 处理resources/read请求
        /// </summary>
        private string HandleResourcesRead(string id, JsonNode paramsNode)
        {
            try
            {
                if (paramsNode == null)
                {
                    return CreateMcpErrorResponse(id, -32602, "Invalid params");
                }

                var paramsObj = paramsNode.ToObject();
                string resourceUri = paramsObj["uri"]?.Value;

                if (string.IsNullOrEmpty(resourceUri))
                {
                    return CreateMcpErrorResponse(id, -32602, "Resource URI is required");
                }

                Log($"[Unity.Mcp] 处理resources/read请求，Resource URI: {resourceUri}");

                if (!availableResources.TryGetValue(resourceUri, out var resource))
                {
                    return CreateMcpErrorResponse(id, -32602, $"Resource not found: {resourceUri}");
                }

                var result = new JsonClass();

                // 构建内容数组
                var contents = new JsonArray();
                var content = new JsonClass();
                content.Add("uri", new JsonData(resource.Url));
                content.Add("mimeType", new JsonData(resource.MimeType));

                // 这里应该读取实际的资源内容，现在先返回描述
                content.Add("text", new JsonData($"Resource: {resource.Name}\nDescription: {resource.Description}\nMimeType: {resource.MimeType}"));

                contents.Add(content);
                result.Add("contents", contents);

                return CreateMcpSuccessResponse(id, result);
            }
            catch (Exception ex)
            {
                LogError($"[Unity.Mcp] 处理resources/read请求失败: {ex.Message}");
                return CreateMcpErrorResponse(id, -32603, $"Internal error: {ex.Message}");
            }
        }

        /// <summary>
        /// 适配工具参数，将MCP格式转换为工具期望的格式
        /// </summary>
        private JsonNode AdaptToolArguments(string toolName, JsonNode argumentsNode)
        {
            try
            {
                if (argumentsNode == null)
                {
                    return new JsonData("null");
                }

                switch (toolName)
                {
                    case "batch_call":
                        // batch_call期望直接的数组格式，但MCP传入的是 {args: [...]}
                        if (argumentsNode is JsonClass argsObj && argsObj.ContainsKey("args"))
                        {
                            var argsArray = argsObj["args"];
                            Log($"[Unity.Mcp] 适配batch_call参数：从对象格式转换为数组格式");
                            return argsArray;
                        }
                        break;

                    case "async_call":
                        // async_call参数保持原样
                        return argumentsNode;

                    default:
                        // 其他工具参数保持原样
                        return argumentsNode;
                }

                return argumentsNode;
            }
            catch (Exception ex)
            {
                LogError($"[Unity.Mcp] 适配工具参数失败 {toolName}: {ex.Message}");
                return argumentsNode; // 失败时返回原参数
            }
        }

        /// <summary>
        /// 处理tools/call请求
        /// </summary>
        private async Task<string> HandleToolsCall(string id, JsonNode paramsNode)
        {
            try
            {
                McpLogger.Log($"[Unity.Mcp] HandleToolsCall开始，ID: {id}");

                if (paramsNode == null)
                {
                    McpLogger.LogError($"[Unity.Mcp] HandleToolsCall参数为null，ID: {id}");
                    return CreateMcpErrorResponse(id, -32602, "Invalid params");
                }

                var paramsObj = paramsNode.ToObject();
                string toolName = paramsObj["name"]?.Value;
                JsonNode argumentsNode = paramsObj["arguments"];

                McpLogger.Log($"[Unity.Mcp] 工具调用 - 工具名: {toolName}, ID: {id}");

                if (string.IsNullOrEmpty(toolName))
                {
                    McpLogger.LogError($"[Unity.Mcp] 工具名为空，ID: {id}");
                    return CreateMcpErrorResponse(id, -32602, "Tool name is required");
                }

                // 检查工具是否启用
                if (!McpLocalSettings.Instance.IsToolEnabled(toolName))
                {
                    McpLogger.LogError($"[Unity.Mcp] 工具已禁用: {toolName}, ID: {id}");
                    return CreateMcpErrorResponse(id, -32602, $"Tool '{toolName}' is disabled");
                }

                // 统一通过GetMcpTool获取工具实例
                Log($"[Unity.Mcp] 获取McpTool实例: {toolName}");
                var tool = GetMcpTool(toolName);
                if (tool == null)
                {
                    LogError($"[Unity.Mcp] 未找到工具: {toolName}");
                    return CreateMcpErrorResponse(id, -32602, $"Tool not found: {toolName}");
                }

                Log($"[Unity.Mcp] 找到工具: {tool.GetType().Name}，开始异步处理命令");

                // 使用TaskCompletionSource来等待异步执行完成
                var tcs = new TaskCompletionSource<JsonNode>();
                var startTime = DateTime.Now;

                // 对特定工具进行参数适配
                JsonNode adaptedArguments = AdaptToolArguments(toolName, argumentsNode);

                // 统一通过HandleCommand处理所有工具调用，使用消息队列确保在主线程中执行
                EnqueueTask(() =>
                {
                    try
                    {
                        tool.HandleCommand(adaptedArguments, (result) =>
                        {
                            var endTime = DateTime.Now;
                            var duration = (endTime - startTime).TotalMilliseconds;

                            try
                            {
                                Log($"[Unity.Mcp] 工具执行完成，结果: {result}");

                                // 记录执行结果到McpExecuteRecordObject
                                try
                                {
                                    var recordObject = McpExecuteRecordObject.instance;

                                    // 根据工具类型决定记录方式
                                    string cmdName;
                                    string argsString;
                                    if (toolName == "async_call")
                                    {
                                        // async_call: 记录具体的func和args
                                        var func = argumentsNode?["func"]?.Value;
                                        if (string.IsNullOrEmpty(func))
                                            func = "checking...";
                                        cmdName = "async_call." + func;
                                        argsString = argumentsNode?.ToString() ?? "{}";
                                    }
                                    else if (toolName == "batch_call" && argumentsNode is JsonClass batchArgs && batchArgs.ContainsKey("args"))
                                    {
                                        var funcsArray = batchArgs["args"].AsArray;
                                        if (funcsArray != null)
                                        {
                                            var funcNames = new List<string>();
                                            foreach (JsonNode funcObj in funcsArray.Childs)
                                            {
                                                var funcNode = funcObj as JsonClass;
                                                if (funcNode != null)
                                                {
                                                    var funcName = funcNode["func"]?.Value;
                                                    if (!string.IsNullOrEmpty(funcName))
                                                    {
                                                        funcNames.Add(funcName);
                                                    }
                                                }
                                            }
                                            if (funcNames.Count > 2)
                                            {
                                                cmdName = $"batch_call.[{string.Join(",", funcNames.Take(2))}...]";
                                            }
                                            else
                                            {
                                                cmdName = $"batch_call.[{string.Join(",", funcNames)}]";
                                            }
                                        }
                                        else
                                        {
                                            cmdName = "batch_call.[*]";
                                        }
                                        argsString = funcsArray?.ToString() ?? "[]";
                                    }
                                    else
                                    {
                                        // 其他工具类型: 使用默认方式
                                        cmdName = toolName;
                                        argsString = Json.FromObject(new { func = cmdName, args = argumentsNode }).ToString();
                                    }

                                    // 动态判断 status：根据 result 中的 success 字段
                                    string error = "";
                                    if (result is JsonClass jsonResult)
                                    {
                                        var successNode = jsonResult["success"];
                                        if (successNode != null && successNode.Value == "false")
                                        {
                                            error = jsonResult["error"]?.Value ?? "Unknown error";
                                        }
                                    }

                                    string resultString = Json.FromObject(new
                                    {
                                        status = "success",
                                        result = Json.FromObject(result)
                                    }).ToString();

                                    recordObject.addRecord(
                                        cmdName,
                                        argsString,
                                        resultString,
                                        error, // 成功时error为空
                                        duration,
                                        "MCP Client"
                                    );
                                    recordObject.saveRecords();
                                }
                                catch (Exception recordEx)
                                {
                                    LogError($"[Unity.Mcp] 记录执行结果时发生错误: {recordEx.Message}");
                                }

                                tcs.SetResult(result);
                            }
                            catch (Exception callbackEx)
                            {
                                LogError($"[Unity.Mcp] 工具回调处理失败: {callbackEx.Message}");
                                tcs.SetException(callbackEx);
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        LogError($"[Unity.Mcp] 主线程执行工具调用失败: {ex.Message}");
                        tcs.SetException(ex);
                    }
                });

                // 等待执行完成
                var toolResult = await tcs.Task;

                // 构建MCP响应
                var responseContent = new JsonArray();

                // 检查工具结果中是否包含resources字段
                JsonNode resourcesNode = null;
                JsonNode cleanedResult = toolResult;

                if (toolResult is JsonClass resultObj && resultObj.ContainsKey("resources"))
                {
                    resourcesNode = resultObj["resources"];

                    // 创建不包含resources的清理版本
                    cleanedResult = new JsonClass();
                    foreach (System.Collections.Generic.KeyValuePair<string, JsonNode> kvp in resultObj)
                    {
                        if (kvp.Key != "resources")
                        {
                            cleanedResult.AsObject.Add(kvp.Key, kvp.Value);
                        }
                    }

                    Log($"[Unity.Mcp] 检测到resources字段: {resourcesNode}");
                }

                // 如果存在resources，处理图片资源
                if (resourcesNode != null && resourcesNode is JsonClass resourceObj)
                {
                    string resourceType = resourceObj["type"]?.Value;
                    string resourcePath = resourceObj["path"]?.Value;
                    string resourceFormat = resourceObj["format"]?.Value;

                    if (resourceType == "image" && !string.IsNullOrEmpty(resourcePath) && System.IO.File.Exists(resourcePath))
                    {
                        try
                        {
                            // 读取图片文件并转换为Base64
                            byte[] imageBytes = System.IO.File.ReadAllBytes(resourcePath);
                            string base64Data = System.Convert.ToBase64String(imageBytes);

                            // 根据格式确定MIME类型
                            string mimeType = "image/jpeg"; // 默认
                            if (!string.IsNullOrEmpty(resourceFormat))
                            {
                                switch (resourceFormat.ToUpperInvariant())
                                {
                                    case "PNG":
                                        mimeType = "image/png";
                                        break;
                                    case "JPG":
                                    case "JPEG":
                                        mimeType = "image/jpeg";
                                        break;
                                    case "GIF":
                                        mimeType = "image/gif";
                                        break;
                                    case "BMP":
                                        mimeType = "image/bmp";
                                        break;
                                    case "WEBP":
                                        mimeType = "image/webp";
                                        break;
                                }
                            }

                            // 创建图片内容
                            var imageContent = new JsonClass();
                            imageContent.Add("type", new JsonData("image"));
                            imageContent.Add("data", new JsonData(base64Data));
                            imageContent.Add("mimeType", new JsonData(mimeType));
                            responseContent.Add(imageContent);

                            Log($"[Unity.Mcp] 添加图片资源到响应: {resourcePath}, MIME: {mimeType}, 大小: {imageBytes.Length} bytes");
                        }
                        catch (Exception ex)
                        {
                            LogError($"[Unity.Mcp] 处理图片资源失败: {ex.Message}");
                        }
                    }
                }

                // 添加文本内容（去除resources的结果）
                var responseTextContent = new JsonClass();
                responseTextContent.Add("type", new JsonData("text"));
                responseTextContent.Add("text", new JsonData(cleanedResult?.ToString() ?? "Tool executed successfully"));
                responseContent.Add(responseTextContent);

                var responseResult = new JsonClass();
                responseResult.Add("content", responseContent);

                return CreateMcpSuccessResponse(id, responseResult);
            }
            catch (Exception ex)
            {
                LogError($"[Unity.Mcp] 工具调用失败: {ex.Message}");
                return CreateMcpErrorResponse(id, -32603, $"Tool execution failed: {ex.Message}");
            }
        }
        /// <summary>
        /// 获取McpTool实例
        /// </summary>
        /// <param name="toolName"></param>
        /// <returns></returns>
        private McpTool GetMcpTool(string toolName)
        {
            Log($"[Unity.Mcp] 请求获取工具: {toolName}");

            if (mcpToolInstanceCache.Count == 0)
            {
                Log($"[Unity.Mcp] 工具缓存为空，开始反射查找工具实例");
                // 没有缓存则反射查找并缓存
                var toolType = typeof(McpTool);
                var toolInstances = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => a.GetTypes())
                    .Where(t => !t.IsAbstract && toolType.IsAssignableFrom(t)).Select(t => Activator.CreateInstance(t) as McpTool);

                int cacheCount = 0;
                foreach (var toolInstance in toolInstances)
                {
                    mcpToolInstanceCache[toolInstance.ToolName] = toolInstance;
                    cacheCount++;
                    Log($"[Unity.Mcp] 缓存工具: {toolInstance.ToolName} ({toolInstance.GetType().Name})");
                }
                Log($"[Unity.Mcp] 工具缓存完成，共缓存 {cacheCount} 个工具");
            }

            if (mcpToolInstanceCache.TryGetValue(toolName, out var tool))
            {
                Log($"[Unity.Mcp] 从缓存中获取到工具: {toolName} ({tool.GetType().Name})");
                return tool;
            }

            if (methodsCall.GetToolMethod(toolName) != null)
            {
                Log($"[Unity.Mcp] 从methodsCall中获取到工具: {toolName}");
                methodsCall.SetToolName(toolName);
                return methodsCall;
            }

            LogError($"[Unity.Mcp] 未找到工具: {toolName}，可用工具: [{string.Join(", ", mcpToolInstanceCache.Keys)}]");
            return null;
        }
        /// <summary>
        /// 创建MCP成功响应
        /// </summary>
        private string CreateMcpSuccessResponse(string id, JsonNode result)
        {
            // 创建标准JSON-RPC 2.0响应
            var response = new JsonClass();
            response.Add("jsonrpc", new JsonData("2.0")); // 必须是字符串"2.0"而不是数字2
            response.Add("result", result);

            // 确保id字段正确处理
            if (id != null)
            {
                // 尝试解析为数字ID
                if (int.TryParse(id, out int numId))
                {
                    response.Add("id", new JsonData(numId));
                }
                else
                {
                    response.Add("id", new JsonData(id));
                }
            }
            else
            {
                response.Add("id", new JsonData(null));
            }

            // 使用System.Text.Json序列化以确保格式正确
            try
            {
                // 将SimpleJson转换为标准JSON字符串
                string jsonString = response.ToString();

                // 验证JSON格式
                if (!jsonString.Contains("\"jsonrpc\":\"2.0\""))
                {
                    // 如果格式不正确，手动构建正确的JSON
                    jsonString = jsonString.Replace("\"jsonrpc\":2.0", "\"jsonrpc\":\"2.0\"");
                }

                McpLogger.Log($"[Unity.Mcp] 发送JSON-RPC响应: {jsonString}");
                return jsonString;
            }
            catch (Exception ex)
            {
                McpLogger.LogError($"[Unity.Mcp] JSON序列化错误: {ex.Message}");
                // 回退到手动构建JSON
                string manualJson = "{{\"jsonrpc\":\"2.0\",\"result\":{0},\"id\":{1}}}";
                string resultJson = result?.ToString() ?? "{}";
                string idJson = id != null ? (int.TryParse(id, out int _) ? id : $"\"{id}\"") : "null";
                string finalJson = string.Format(manualJson, resultJson, idJson);
                McpLogger.Log($"[Unity.Mcp] 手动构建的JSON-RPC响应: {finalJson}");
                return finalJson;
            }
        }

        /// <summary>
        /// 创建MCP错误响应
        /// </summary>
        private string CreateMcpErrorResponse(string id, int code, string message)
        {
            var error = new JsonClass();
            error.Add("code", new JsonData(code));
            error.Add("message", new JsonData(message));

            var response = new JsonClass();
            response.Add("jsonrpc", new JsonData("2.0")); // 必须是字符串"2.0"而不是数字2
            response.Add("error", error);

            // 确保id字段正确处理
            if (id != null)
            {
                // 尝试解析为数字ID
                if (int.TryParse(id, out int numId))
                {
                    response.Add("id", new JsonData(numId));
                }
                else
                {
                    response.Add("id", new JsonData(id));
                }
            }
            else
            {
                response.Add("id", new JsonData(null));
            }

            // 使用System.Text.Json序列化以确保格式正确
            try
            {
                // 将SimpleJson转换为标准JSON字符串
                string jsonString = response.ToString();

                // 验证JSON格式
                if (!jsonString.Contains("\"jsonrpc\":\"2.0\""))
                {
                    // 如果格式不正确，手动构建正确的JSON
                    jsonString = jsonString.Replace("\"jsonrpc\":2.0", "\"jsonrpc\":\"2.0\"");
                }

                McpLogger.Log($"[Unity.Mcp] 发送JSON-RPC错误响应: {jsonString}");
                return jsonString;
            }
            catch (Exception ex)
            {
                McpLogger.LogError($"[Unity.Mcp] JSON序列化错误: {ex.Message}");
                // 回退到手动构建JSON
                string manualJson = "{{\"jsonrpc\":\"2.0\",\"error\":{{\"code\":{0},\"message\":\"{1}\"}},\"id\":{2}}}";
                string idJson = id != null ? (int.TryParse(id, out int _) ? id : $"\"{id}\"") : "null";
                string finalJson = string.Format(manualJson, code, message.Replace("\"", "\\\""), idJson);
                McpLogger.Log($"[Unity.Mcp] 手动构建的JSON-RPC错误响应: {finalJson}");
                return finalJson;
            }
        }

    }

    // HTTP请求记录信息类已移动到McpExecuteRecordObject.HttpRequestRecord
}


