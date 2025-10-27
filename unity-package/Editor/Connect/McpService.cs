using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Unity.Mcp.Models;
using Unity.Mcp.Executer;
using System.Collections;

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

        private List<HttpListener> listeners = new List<HttpListener>();
        private bool isRunning = false;
        private readonly object lockObj = new();
        private Dictionary<
            string,
            (string commandJson, TaskCompletionSource<string> tcs)
        > commandQueue = new();
        
        // 添加取消令牌源，用于优雅地取消异步操作
        private CancellationTokenSource cancellationTokenSource;
         public static readonly int unityPortStart = 8100; // Start of port range
        public static readonly int unityPortEnd = 8102;   // End of port range
        private List<int> activePorts = new(); // Successfully started ports
        public bool IsRunning => isRunning;
        

        // 缓存McpTool类型和实例
        private readonly Dictionary<string, McpTool> mcpToolInstanceCache = new();
        //通用函数执行
        private ToolsCall methodsCall = new ToolsCall();

        // 获取所有活动端口的只读列表
        public List<int> ActivePorts
        {
            get
            {
                lock (lockObj)
                {
                    return new List<int>(activePorts);
                }
            }
        }

        // 静态访问器，方便外部调用
        public static List<int> GetActivePorts()
        {
            return Instance.ActivePorts;
        }

        // 客户端连接状态跟踪
        private readonly Dictionary<string, ClientInfo> connectedClients = new();
        private readonly object clientsLock = new();

        public int ConnectedClientCount
        {
            get
            {
                lock (clientsLock)
                {
                    return connectedClients.Count;
                }
            }
        }

        [InitializeOnLoadMethod]
        static void AutoInit()
        {
            // 确保在编辑器中运行时，McpService 的静态构造函数被调用
            if (Application.isEditor)
            {
                _ =  Instance;
            }
        }

        // 实例析构函数
        ~McpService()
        {
            AssemblyReloadEvents.beforeAssemblyReload -= ForceStop;
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

        public List<ClientInfo> GetConnectedClients()
        {
            lock (clientsLock)
            {
                return connectedClients.Values.ToList();
            }
        }

        // 静态访问器，方便外部调用
        public static List<ClientInfo> GetAllConnectedClients()
        {
            return Instance.GetConnectedClients();
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
            // 初始化实例
            if (EditorPrefs.HasKey("mcp_open_state") && EditorPrefs.GetBool("mcp_open_state"))
            {
                CoroutineRunner.StartCoroutine(StartServiceDelay());
            }
            //监听程序集刷新前事件
            AssemblyReloadEvents.beforeAssemblyReload += ForceStop;
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
            
            if (isRunning)
            {
                Stop();
            }

            // 额外确保 HttpListener 被释放
            if (listeners.Count > 0)
            {
                foreach (var l in listeners)
                {
                    try
                    {
                        // 先停止接受新请求
                        if (l.IsListening)
                        {
                            l.Stop();
                        }
                        
                        // 等待一小段时间让正在处理的请求完成
                        Thread.Sleep(100);
                        
                        // 强制关闭监听器
                        l.Close();
                        
                        // 释放资源
                        ((IDisposable)l).Dispose();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[Unity.Mcp] 强制关闭 HttpListener 时发生错误: {ex.Message}");
                    }
                }
                listeners.Clear();
                Debug.Log("[Unity.Mcp] 所有 HttpListeners 已强制关闭");
            }

            // 强制清理命令队列
            lock (lockObj)
            {
                foreach (var kvp in commandQueue)
                {
                    try
                    {
                        kvp.Value.tcs.TrySetCanceled();
                    }
                    catch { }
                }
                commandQueue.Clear();
            }

            // 清空客户端连接信息
            lock (clientsLock)
            {
                connectedClients.Clear();
            }

            // 确保标记为已停止
            isRunning = false;
            activePorts.Clear();
            
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
                // 不再通过实际创建监听来检测端口占用，直接认为端口可用（不检测）
                return false;
            }
            catch
            {
                return true; // 端口被占用
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
            // 从EditorPrefs读取日志设置，默认为false
            var enableLog = EditorPrefs.GetBool("mcp_enable_log", false);
            Debug.Log($"[Unity.Mcp] <color=green>正在启动UnityMcp HTTP服务器...</color>");

            // 确保先停止现有服务
            Stop();

            // 等待一小段时间确保资源释放
            System.Threading.Thread.Sleep(200);

            if (isRunning)
            {
                Debug.Log($"[Unity.Mcp] 服务已在运行中");
                return;
            }

            // 初始化取消令牌
            cancellationTokenSource?.Cancel();
            cancellationTokenSource?.Dispose();
            cancellationTokenSource = new CancellationTokenSource();

            // 创建 HttpListener 并尝试监听所有端口
            listeners.Clear();
            activePorts.Clear();

            // 记录被占用的端口
            List<int> occupiedPorts = new List<int>();

            // 尝试为每个端口创建一个监听器
            for (int port = unityPortStart; port <= unityPortEnd; port++)
            {
                if (IsPortInUse(port))
                {
                    Debug.LogWarning($"[Unity.Mcp] <color=orange>端口 {port} 被占用</color>");
                    occupiedPorts.Add(port);
                    continue;
                }

                try
                {
                    var listener = new HttpListener();
                    string prefix = $"http://127.0.0.1:{port}/";
                    listener.Prefixes.Add(prefix);
                    listener.Start();
                    listeners.Add(listener);
                    activePorts.Add(port);
                    Task.Run(async () => await ListenerLoop(listener, cancellationTokenSource.Token));
                    Log($"[Unity.Mcp] 成功在端口 {port} 启动监听。");
                    break;
                }
                catch (Exception ex)
                {
                    LogWarning($"[Unity.Mcp] 无法在端口 {port} 启动监听: {ex.Message}");
                }
            }

            if (activePorts.Count == 0)
            {
                Debug.LogError($"[Unity.Mcp] <color=red>无法添加任何监听端口 ({unityPortStart}-{unityPortEnd})，所有端口都被占用或启动失败</color>");

                // 显示占用端口信息
                if (occupiedPorts.Count > 0)
                {
                    Debug.LogError($"[Unity.Mcp] <color=red>被占用的端口: {string.Join(", ", occupiedPorts)}</color>");
                    Debug.LogError("[Unity.Mcp] <color=red>请尝试重启Unity编辑器以释放端口</color>");
                }

                return;
            }

            isRunning = true;
            // 增加更明显的启动日志，不受EnableLog影响
            string portsInfo = string.Join(", ", activePorts);
            Debug.Log($"[Unity.Mcp] <color=green>HTTP服务器成功启动!</color> 监听端口: {portsInfo}");

            // 保存状态到 EditorPrefs
            EditorPrefs.SetBool("mcp_open_state", true);

            // Start the command processing
            EditorApplication.update += ProcessCommands;
            Log($"[Unity.Mcp] 启动完成，命令处理已注册");
        }

        // 静态方法，方便外部调用
        public static void StartService()
        {
            Instance.Start();
        }

        // 实例方法
        public void Stop()
        {
            if (!isRunning && listeners.Count == 0)
            {
                return;
            }

            Debug.Log($"[Unity.Mcp] <color=orange>正在停止UnityMcp HTTP服务器...</color>");

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

                // 先移除命令处理器
                EditorApplication.update -= ProcessCommands;

                // 清空命令队列
                lock (lockObj)
                {
                    foreach (var kvp in commandQueue)
                    {
                        try
                        {
                            // 为所有等待中的命令设置取消结果
                            kvp.Value.tcs.TrySetResult(Json.FromObject(new
                            {
                                status = "error",
                                error = "Server shutting down"
                            }));
                        }
                        catch { }
                    }
                    commandQueue.Clear();
                }

                // 清空客户端连接信息
                lock (clientsLock)
                {
                    connectedClients.Clear();
                }

                // 关闭和释放 HttpListener
                if (listeners.Count > 0)
                {
                    foreach (var l in listeners)
                    {
                        try
                        {
                            // 先停止接受新请求
                            if (l.IsListening)
                            {
                                l.Stop();
                            }
                            
                            // 等待一小段时间让正在处理的请求完成
                            Thread.Sleep(50);
                            
                            // 关闭监听器
                            l.Close();
                            
                            // 释放资源
                            ((IDisposable)l).Dispose();
                        }
                        catch (Exception listenerEx)
                        {
                            Debug.LogError($"[Unity.Mcp] 关闭 HttpListener 时发生错误: {listenerEx.Message}");
                        }
                    }
                    listeners.Clear();
                    McpLogger.Log("[Unity.Mcp] 所有 HttpListeners 已关闭");
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
                activePorts.Clear();

                Debug.Log($"[Unity.Mcp] <color=orange>服务已停止，HTTP监听器已关闭，命令处理已注销</color>");

                // 等待一小段时间确保资源释放
                System.Threading.Thread.Sleep(100);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Unity.Mcp] 停止服务时发生错误: {ex.Message}");

                // 确保状态正确
                isRunning = false;
                listeners.Clear();
                activePorts.Clear();
            }
        }

        // 静态方法，方便外部调用
        public static void StopService()
        {
            Instance.Stop();
        }

        private async Task ListenerLoop(HttpListener listener, CancellationToken cancellationToken)
        {
            Log($"[Unity.Mcp] HTTP监听循环已启动 on port {(listener.Prefixes.FirstOrDefault() ?? "N/A").Split(':')[2].Trim('/')}");

            while (isRunning && listener.IsListening && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    Log($"[Unity.Mcp] 等待HTTP请求...");
                    
                    // 使用带取消令牌的GetContextAsync
                    var context = await listener.GetContextAsync();

                    // Fire and forget each HTTP request with cancellation token
                    _ = HandleHttpRequestAsync(context, cancellationToken);
                }
                catch (HttpListenerException ex)
                {
                    if (isRunning && !cancellationToken.IsCancellationRequested)
                    {
                        LogError($"[Unity.Mcp] HTTP监听器错误: {ex.Message}");
                    }
                    else
                    {
                        Log($"[Unity.Mcp] HTTP监听器已停止");
                    }
                    break; // Exit loop if listener is closed or has an error
                }
                catch (ObjectDisposedException)
                {
                    Log($"[Unity.Mcp] HTTP监听器已被释放，循环终止。");
                    break; // Exit loop if listener is disposed
                }
                catch (OperationCanceledException)
                {
                    Log($"[Unity.Mcp] HTTP监听循环已被取消。");
                    break; // Exit loop if operation is cancelled
                }
                catch (Exception ex)
                {
                    if (isRunning && !cancellationToken.IsCancellationRequested)
                    {
                        LogError($"[Unity.Mcp] 监听器错误: {ex.Message}");
                    }
                }
            }

            Log($"[Unity.Mcp] HTTP监听循环已结束 on port {(listener.Prefixes.FirstOrDefault() ?? "N/A").Split(':')[2].Trim('/')}");
        }

        /// <summary>
        /// 处理 HTTP 请求
        /// </summary>
        private async Task HandleHttpRequestAsync(HttpListenerContext context, CancellationToken cancellationToken)
        {
            string clientEndpoint = context.Request.RemoteEndPoint?.ToString() ?? "Unknown";
            string clientId = Guid.NewGuid().ToString();
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;

            // 增强请求日志，不受EnableLog影响
            McpLogger.Log($"[Unity.Mcp] <color=cyan>收到HTTP请求:</color> {request.HttpMethod} {request.Url} from {clientEndpoint} (ID: {clientId})");

            try
            {
                // 设置响应头，允许跨域
                response.Headers.Add("Access-Control-Allow-Origin", "*");
                response.Headers.Add("Access-Control-Allow-Methods", "POST, GET, OPTIONS");
                response.Headers.Add("Access-Control-Allow-Headers", "Content-Type");
                response.ContentType = "application/json";
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

                // 只接受 POST 和 GET 请求
                if (request.HttpMethod != "POST" && request.HttpMethod != "GET")
                {
                    response.StatusCode = 405; // Method Not Allowed
                    byte[] errorBytes = Encoding.UTF8.GetBytes(
                        /*lang=json,strict*/
                        "{\"status\":\"error\",\"error\":\"Only POST and GET methods are supported\"}"
                    );
                    await response.OutputStream.WriteAsync(errorBytes, 0, errorBytes.Length);
                    response.Close();
                    LogWarning($"[Unity.Mcp] 不支持的HTTP方法: {request.HttpMethod} from {clientEndpoint}");
                    return;
                }

                string commandText = "";

                // GET 请求 - 简单的 ping 检查
                if (request.HttpMethod == "GET")
                {
                    commandText = "ping";
                    Log($"[Unity.Mcp] GET请求，处理为ping命令");
                }
                // POST 请求 - 读取请求体
                else if (request.HttpMethod == "POST")
                {
                    using (StreamReader reader = new StreamReader(request.InputStream, request.ContentEncoding))
                    {
                        commandText = await reader.ReadToEndAsync();
                    }
                    Log($"[Unity.Mcp] 接收到POST命令 from {clientEndpoint}: {commandText}");
                }

                // 添加客户端到连接列表（用于统计）
                var clientInfo = new ClientInfo
                {
                    Id = clientId,
                    EndPoint = clientEndpoint,
                    ConnectedAt = DateTime.Now,
                    LastActivity = DateTime.Now,
                    CommandCount = 0,
                    CommandText = commandText
                };

                lock (clientsLock)
                {
                    connectedClients[clientId] = clientInfo;
                    if (connectedClients.TryGetValue(clientId, out var existingClient))
                    {
                        existingClient.LastActivity = DateTime.Now;
                        existingClient.CommandCount++;
                    }
                }

                // 快速处理 ping 命令
                if (commandText.Trim() == "ping")
                {
                    Log($"[Unity.Mcp] 处理ping命令 from {clientEndpoint}");
                    byte[] pingResponseBytes = Encoding.UTF8.GetBytes(
                        /*lang=json,strict*/
                        "{\"status\":\"success\",\"result\":{\"message\":\"pong\"}}"
                    );
                    response.StatusCode = 200;
                    await response.OutputStream.WriteAsync(pingResponseBytes, 0, pingResponseBytes.Length);
                    response.Close();
                    Log($"[Unity.Mcp] ping响应已发送 to {clientEndpoint}");

                    // 从连接列表中移除客户端
                    lock (clientsLock)
                    {
                        connectedClients.Remove(clientId);
                    }
                    return;
                }

                // 普通命令 - 加入队列处理
                string commandIdInQueue = Guid.NewGuid().ToString();
                TaskCompletionSource<string> tcs = new();

                lock (lockObj)
                {
                    commandQueue[commandIdInQueue] = (commandText, tcs);
                    Log($"[Unity.Mcp] 命令已加入队列 ID: {commandIdInQueue}");
                }

                // 等待命令处理完成
                string responseJson = await tcs.Task;
                byte[] responseBytes = Encoding.UTF8.GetBytes(responseJson);

                response.StatusCode = 200;
                await response.OutputStream.WriteAsync(responseBytes, 0, responseBytes.Length);
                response.Close();
                Log($"[Unity.Mcp] 响应已发送 to {clientEndpoint}, ID: {commandIdInQueue}");

                // 从连接列表中移除客户端
                lock (clientsLock)
                {
                    connectedClients.Remove(clientId);
                }
            }
            catch (Exception ex)
            {
                LogError($"[Unity.Mcp] HTTP请求处理异常 {clientEndpoint}: {ex.Message}");

                try
                {
                    if (!response.OutputStream.CanWrite)
                    {
                        return;
                    }

                    response.StatusCode = 500;
                    string errorMessage = ex.Message.Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
                    byte[] errorBytes = Encoding.UTF8.GetBytes(
                        /*lang=json,strict*/
                        $"{{\"status\":\"error\",\"error\":\"{errorMessage}\"}}"
                    );
                    await response.OutputStream.WriteAsync(errorBytes, 0, errorBytes.Length);
                    response.Close();
                }
                catch
                {
                    // 无法发送错误响应，忽略
                }

                // 从连接列表中移除客户端
                lock (clientsLock)
                {
                    connectedClients.Remove(clientId);
                }
            }
        }

        private void ProcessCommands()
        {
            List<string> processedIds = new();
            lock (lockObj)
            {
                if (commandQueue.Count > 0)
                {
                    Log($"[Unity.Mcp] 开始处理命令队列，队列长度: {commandQueue.Count}");
                }

                foreach (
                    KeyValuePair<
                        string,
                        (string commandJson, TaskCompletionSource<string> tcs)
                    > kvp in commandQueue.ToList()
                )
                {
                    string id = kvp.Key;
                    string commandText = kvp.Value.commandJson;
                    TaskCompletionSource<string> tcs = kvp.Value.tcs;

                    Log($"[Unity.Mcp] 处理命令 ID: {id}");

                    try
                    {
                        // Special case handling
                        if (string.IsNullOrEmpty(commandText))
                        {
                            LogWarning($"[Unity.Mcp] 接收到空命令 ID: {id}");
                            var emptyResponse = new
                            {
                                status = "error",
                                error = "Empty command received",
                            };
                            tcs.SetResult(Json.FromObject(emptyResponse));
                            processedIds.Add(id);
                            continue;
                        }

                        // Trim the command text to remove any whitespace
                        commandText = commandText.Trim();

                        // Non-Json direct commands handling (like ping)
                        if (commandText == "ping")
                        {
                            Log($"[Unity.Mcp] 处理ping命令 ID: {id}");
                            var pingResponse = new
                            {
                                status = "success",
                                result = new { message = "pong" },
                            };
                            string pingResponseJson = Json.FromObject(pingResponse);
                            tcs.SetResult(pingResponseJson);
                            Log($"[Unity.Mcp] ping命令处理完成 ID: {id}");
                            processedIds.Add(id);
                            continue;
                        }

                        // Check if the command is valid Json before attempting to deserialize
                        if (!IsValidJson(commandText))
                        {
                            LogError($"[Unity.Mcp] 无效JSON格式 ID: {id}, Content: {commandText}");
                            var invalidJsonResponse = new
                            {
                                status = "error",
                                error = "Invalid Json format",
                                receivedText = commandText.Length > 50
                                    ? commandText[..50] + "..."
                                    : commandText,
                            };
                            tcs.SetResult(Json.FromObject(invalidJsonResponse));
                            processedIds.Add(id);
                            continue;
                        }

                        // Normal Json command processing
                        Log($"[Unity.Mcp] 开始解析JSON命令 ID: {id}");
                        Command command = DeserializeCommand(commandText);
                        if (command == null)
                        {
                            LogError($"[Unity.Mcp] 命令反序列化为null ID: {id}");
                            var nullCommandResponse = new
                            {
                                status = "error",
                                error = "Command deserialized to null",
                                details = "The command was valid Json but could not be deserialized to a Command object",
                            };
                            tcs.SetResult(Json.FromObject(nullCommandResponse));
                        }
                        else
                        {
                            Log($"[Unity.Mcp] 执行命令 ID: {id}, Type: {command.type}");
                            // 异步执行命令，但不等待结果，让它在后台执行
                            try
                            {
                                ExecuteCommand(command, tcs);
                            }
                            catch (Exception asyncEx)
                            {
                                LogError($"[Unity.Mcp] 异步执行命令时发生错误 ID: {id}: {asyncEx.Message}\n{asyncEx.StackTrace}");
                                var response = new
                                {
                                    status = "error",
                                    error = asyncEx.Message,
                                    commandType = command?.type ?? "Unknown",
                                    details = "Error occurred during async command execution"
                                };
                                string responseJson = Json.FromObject(response);
                                tcs.SetResult(responseJson);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogError($"[Unity.Mcp] 处理命令时发生错误 ID: {id}: {ex.Message}\n{ex.StackTrace}");

                        var response = new
                        {
                            status = "error",
                            error = ex.Message,
                            commandType = "Unknown (error during processing)",
                            receivedText = commandText?.Length > 50
                                ? commandText[..50] + "..."
                                : commandText,
                        };
                        string responseJson = Json.FromObject(response);
                        tcs.SetResult(responseJson);
                        Log($"[Unity.Mcp] 错误响应已设置 ID: {id}");
                    }

                    processedIds.Add(id);
                }

                foreach (string id in processedIds)
                {
                    commandQueue.Remove(id);
                }

                if (processedIds.Count > 0)
                {
                    Log($"[Unity.Mcp] 命令队列处理完成，已处理: {processedIds.Count} 个命令");
                }
            }
        }

        // Helper method to check if a string is valid Json
        private static bool IsValidJson(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            text = text.Trim();
            if (
                (text.StartsWith("{") && text.EndsWith("}"))
                || // Object
                (text.StartsWith("[") && text.EndsWith("]"))
            ) // Array
            {
                try
                {
                    Json.Parse(text);
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            return false;
        }

        private void ExecuteCommand(Command command, TaskCompletionSource<string> tcs)
        {
            Log($"[Unity.Mcp] 开始执行命令: Type={command.type}");

            try
            {
                if (string.IsNullOrEmpty(command.type))
                {
                    LogError($"[Unity.Mcp] 命令类型为空");
                    var errorResponse = new
                    {
                        status = "error",
                        error = "Command type cannot be empty",
                        details = "A valid command type is required for processing",
                    };
                    tcs.SetResult(Json.FromObject(errorResponse));
                    return;
                }

                // Handle ping command for connection verification
                if (command.type.Equals("ping", StringComparison.OrdinalIgnoreCase))
                {
                    Log($"[Unity.Mcp] 处理ping命令");
                    var pingResponse = new
                    {
                        status = "success",
                        result = new { message = "pong" },
                    };
                    Log($"[Unity.Mcp] ping命令执行成功");
                    tcs.SetResult(Json.FromObject(pingResponse));
                    return;
                }

                // Use JsonClass for args as the new handlers likely expect this
                JsonNode paramsObject = command.cmd ?? new JsonData("null");

                Log($"[Unity.Mcp] 命令参数: {paramsObject}");

                Log($"[Unity.Mcp] 获取McpTool实例: {command.type}");
                var tool = GetMcpTool(command.type);
                if (tool == null)
                {
                    LogError($"[Unity.Mcp] 未找到工具: {command.type}");
                    throw new ArgumentException($"Unknown or unsupported command type: {command.type}");
                }

                Log($"[Unity.Mcp] 找到工具: {tool.GetType().Name}，开始异步处理命令");
                var startTime = System.DateTime.Now;
                tool.HandleCommand(paramsObject, (result) =>
                {
                    var endTime = System.DateTime.Now;
                    var duration = (endTime - startTime).TotalMilliseconds;

                    string re;
                    try
                    {
                        re = Json.FromObject(new
                        {
                            status = "success",
                            result = Json.FromObject(result)
                        }).ToString();
                        Log($"[Unity.Mcp] 工具执行完成，结果: {re}");
                    }
                    catch (Exception serEx)
                    {
                        LogError($"[Unity.Mcp] 序列化响应失败: {serEx.Message}");
                        // 尝试序列化一个简化的错误响应
                        re = Json.FromObject((object)(new
                        {
                            status = "error",
                            error = $"Failed to serialize response: {serEx.Message}",
                            details = result?.GetType().ToString() ?? "null"
                        }));
                    }
                    // 记录执行结果到McpExecuteRecordObject
                    try
                    {
                        var recordObject = McpExecuteRecordObject.instance;

                        // 根据命令类型决定记录方式
                        string cmdName;
                        string argsString;
                        if (command.type == "async_call")
                        {
                            // function_call: 记录具体的func和args
                            var func = paramsObject["func"].Value;
                            if(string.IsNullOrEmpty(func))
                                func = "checking...";
                            cmdName = "async_call." + func;
                            argsString = paramsObject.ToPrettyString();
                        }
                        else if (command.type == "batch_call" && paramsObject is JsonArray funcsArray)
                        {
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
                            argsString = paramsObject.ToPrettyString();
                        }
                        else
                        {
                            // 其他命令类型: 使用默认方式
                            cmdName = command.type;
                            // 修正为标准JSON格式
                            argsString = Json.FromObject((object)(new { func = cmdName, args = paramsObject })).ToPrettyString();
                        }

                        // 动态判断 status：根据 result 中的 success 字段
                        string error = "";
                        if (result is JsonClass jsonResult)
                        {
                            var successNode = jsonResult["success"];
                            if (successNode != null && successNode.Value == "false")
                            {
                                error = jsonResult["error"].Value;
                            }
                        }
                        recordObject.addRecord(
                            cmdName,
                            argsString,
                            re,
                            error, // 成功时error为空
                            duration,
                            "MCP Client"
                        );
                        recordObject.saveRecords();
                    }
                    catch (System.Exception recordEx)
                    {
                        LogError($"[Unity.Mcp] 记录执行结果时发生错误: {recordEx.Message}");
                    }

                    tcs.SetResult(re);
                });
            }
            catch (Exception ex)
            {
                // Log the detailed error in Unity for debugging
                Debug.LogException(new Exception(
                    $"[Unity.Mcp] 执行命令时发生错误 '{command?.type ?? "Unknown"}': {ex.Message}\n{ex.StackTrace}",
                    ex
                ));

                // Standard error response format
                var response = new
                {
                    status = "error",
                    error = ex.Message, // Provide the specific error message
                    command = command?.type ?? "Unknown", // Include the command type if available
                    stackTrace = ex.StackTrace, // Include stack trace for detailed debugging
                    paramsSummary = command?.cmd != null
                        ? command.cmd.Value
                        : "No args", // Summarize args for context
                };
                Log($"[Unity.Mcp] 错误响应已生成: Type={command?.type ?? "Unknown"}");
                var errorResponse = Json.FromObject(response);

                // 记录错误执行结果到McpExecuteRecordObject
                try
                {
                    var recordObject = McpExecuteRecordObject.instance;

                    string cmdName;
                    string argsString;

                    if (command?.type == "async_call" && command.cmd is JsonClass singleCallParams)
                    {
                        cmdName = "async_call." + (singleCallParams["func"]?.Value ?? "Unknown");
                        argsString = singleCallParams.ToPrettyString();
                    }
                    else if (command?.type == "batch_call" && command.cmd is JsonArray funcsArray)
                    {
                        // batch_call 记录func名拼接
                        var funcNames = new List<string>();
                        foreach (JsonNode funcObj in funcsArray.Childs)
                        {
                            var funcNode = funcObj as JsonClass;
                            if (funcNode != null)
                            {
                                var funcName = funcNode["func"]?.Value;
                                if (!string.IsNullOrEmpty(funcName))
                                    funcNames.Add(funcName);
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
                        argsString = funcsArray.ToPrettyString();
                    }
                    else
                    {
                        cmdName = command?.type ?? "Unknown";
                        argsString = command?.cmd != null ? command.cmd.ToPrettyString() : "{}";
                    }

                    recordObject.addRecord(
                        cmdName,
                        argsString,
                        ex.Message,
                        ex.Message,
                        0,
                        "MCP Client"
                    );
                    recordObject.saveRecords();
                }
                catch (System.Exception recordEx)
                {
                    LogError($"[Unity.Mcp] 记录错误执行结果时发生错误: {recordEx.Message}");
                }

                tcs.SetResult(errorResponse);
                return;
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
        /// SimpleJson 反序列化 Command 辅助方法
        /// </summary>
        private static Command DeserializeCommand(string json)
        {
            try
            {
                var node = Json.Parse(json);
                if (node == null) return null;

                var jsonObj = node.ToObject();
                if (jsonObj == null) return null;

                var command = new Command
                {
                    type = jsonObj["type"]?.Value,
                    cmd = jsonObj["cmd"]
                };
                return command;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Unity.Mcp] 反序列化 Command 失败: {ex.Message}");
                return null;
            }
        }
    }
    
   // 客户端信息类
   public class ClientInfo
   {
       public string Id { get; set; }
       public string EndPoint { get; set; }
       public DateTime ConnectedAt { get; set; }
       public DateTime LastActivity { get; set; }
       public int CommandCount { get; set; }
       public string CommandText { get; set; }
   }
}


