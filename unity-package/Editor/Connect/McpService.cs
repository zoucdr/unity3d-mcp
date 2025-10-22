using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Unity.Mcp.Models;
using Unity.Mcp.Executer;

namespace Unity.Mcp
{
    [InitializeOnLoad]
    public static partial class McpService
    {
        private static TcpListener listener;
        private static bool isRunning = false;
        private static readonly object lockObj = new();
        private static Dictionary<
            string,
            (string commandJson, TaskCompletionSource<string> tcs)
        > commandQueue = new();
        public static readonly int unityPortStart = 8100; // Start of port range
        public static readonly int unityPortEnd = 8110;   // End of port range
        public static int currentPort = -1; // Currently used port
        public static bool IsRunning => isRunning;

        // 客户端连接状态跟踪
        private static readonly Dictionary<string, ClientInfo> connectedClients = new();
        private static readonly object clientsLock = new();

        public static int ConnectedClientCount
        {
            get
            {
                lock (clientsLock)
                {
                    return connectedClients.Count;
                }
            }
        }

        public static List<ClientInfo> GetConnectedClients()
        {
            lock (clientsLock)
            {
                return connectedClients.Values.ToList();
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
        }

        // 缓存McpTool类型和实例，静态工具类型
        private static readonly Dictionary<string, McpTool> mcpToolInstanceCache = new();
        //通用函数执行
        private static ToolsCall methodsCall = new ToolsCall();
        // 在 Unity.Mcp 类中添加日志开关
        public static bool EnableLog = false;

        // 统一的日志输出方法
        private static void Log(string message)
        {
            if (EnableLog) Debug.Log(message);
        }

        private static void LogWarning(string message)
        {
            if (EnableLog) Debug.LogWarning(message);
        }

        private static void LogError(string message)
        {
            if (EnableLog) Debug.LogError(message);
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

        static McpService()
        {
            EditorApplication.quitting += Stop;
            AssemblyReloadEvents.beforeAssemblyReload += Stop;
            if (EditorPrefs.HasKey("mcp_open_state") && EditorPrefs.GetBool("mcp_open_state"))
            {
                Start();
            }
        }

        public static void Start()
        {
            // 从EditorPrefs读取日志设置，默认为false
            McpService.EnableLog = EditorPrefs.GetBool("mcp_enable_log", false);
            Log($"[Unity.Mcp] 正在启动UnityMcp...");
            Stop();

            if (isRunning)
            {
                Log($"[Unity.Mcp] 服务已在运行中");
                return;
            }

            // Try to start listener on available port in range
            bool started = false;
            SocketException lastException = null;

            for (int port = unityPortStart; port <= unityPortEnd; port++)
            {
                try
                {
                    Log($"[Unity.Mcp] 尝试在端口 {port} 启动TCP监听器...");
                    listener = new TcpListener(IPAddress.Loopback, port);
                    listener.Start();
                    currentPort = port;
                    isRunning = true;
                    started = true;
                    Log($"[Unity.Mcp] TCP监听器已成功启动，端口: {port}");

                    // Start the listener loop and command processing
                    Task.Run(ListenerLoop);
                    EditorApplication.update += ProcessCommands;
                    Log($"[Unity.Mcp] 启动完成，监听循环已开始，命令处理已注册");
                    break;
                }
                catch (SocketException ex)
                {
                    lastException = ex;
                    if (ex.SocketErrorCode == SocketError.AddressAlreadyInUse)
                    {
                        Log($"[Unity.Mcp] 端口 {port} 已被占用，尝试下一个端口...");
                    }
                    else
                    {
                        LogWarning($"[Unity.Mcp] 端口 {port} 启动失败: {ex.Message}，尝试下一个端口...");
                    }

                    // Clean up failed listener
                    try
                    {
                        listener?.Stop();
                    }
                    catch { }
                    listener = null;
                }
            }

            if (!started)
            {
                LogError($"[Unity.Mcp] 无法在端口范围 {unityPortStart}-{unityPortEnd} 内启动TCP监听器。最后错误: {lastException?.Message}");
                if (lastException?.SocketErrorCode == SocketError.AddressAlreadyInUse)
                {
                    LogError("[Unity.Mcp] 所有端口都被占用。请确保没有其他Unity MCP实例正在运行。");
                }
            }
        }

        public static void Stop()
        {
            if (!isRunning)
            {
                return;
            }

            Log($"[Unity.Mcp] 正在停止UnityMcp...");

            try
            {
                listener?.Stop();
                listener = null;
                isRunning = false;
                currentPort = -1; // Reset current port

                // 清空客户端连接信息
                lock (clientsLock)
                {
                    connectedClients.Clear();
                }

                EditorApplication.update -= ProcessCommands;
                Log($"[Unity.Mcp] 服务已停止，TCP监听器已关闭，命令处理已注销");
            }
            catch (Exception ex)
            {
                LogError($"[Unity.Mcp] 停止服务时发生错误: {ex.Message}");
            }
        }

        private static async Task ListenerLoop()
        {
            Log($"[Unity.Mcp] 监听循环已启动");

            while (isRunning)
            {
                try
                {
                    Log($"[Unity.Mcp] 等待客户端连接...");
                    TcpClient client = await listener.AcceptTcpClientAsync();

                    // Enable basic socket keepalive
                    client.Client.SetSocketOption(
                        SocketOptionLevel.Socket,
                        SocketOptionName.KeepAlive,
                        true
                    );

                    // Set longer receive timeout to prevent quick disconnections
                    client.ReceiveTimeout = 60000; // 60 seconds

                    Log($"[Unity.Mcp] 客户端连接配置完成：KeepAlive=true, ReceiveTimeout=60s");

                    // Fire and forget each client connection
                    _ = HandleClientAsync(client);
                }
                catch (Exception ex)
                {
                    if (isRunning)
                    {
                        LogError($"[Unity.Mcp] 监听器错误: {ex.Message}");
                    }
                    else
                    {
                        Log($"[Unity.Mcp] 监听器已停止");
                    }
                }
            }

            Log($"[Unity.Mcp] 监听循环已结束");
        }

        /// <summary>
        /// 从流中读取指定字节数的数据
        /// </summary>
        private static async Task<byte[]> ReadExactAsync(NetworkStream stream, int count)
        {
            byte[] buffer = new byte[count];
            int totalBytesRead = 0;

            while (totalBytesRead < count)
            {
                int bytesRead = await stream.ReadAsync(buffer, totalBytesRead, count - totalBytesRead);
                if (bytesRead == 0)
                {
                    // 使用自定义异常来标识这是正常的连接关闭
                    throw new ConnectionClosedException($"Connection closed gracefully. Expected {count} bytes, received {totalBytesRead}");
                }
                totalBytesRead += bytesRead;
            }

            return buffer;
        }

        /// <summary>
        /// 连接关闭异常（正常关闭，不是错误）
        /// </summary>
        private class ConnectionClosedException : Exception
        {
            public ConnectionClosedException(string message) : base(message) { }
        }

        /// <summary>
        /// 发送带长度前缀的数据
        /// </summary>
        private static async Task SendWithLengthAsync(NetworkStream stream, byte[] data)
        {
            uint dataLength = (uint)data.Length;

            // 手动构建大端序字节数组
            byte[] lengthBytes = new byte[4];
            lengthBytes[0] = (byte)((dataLength >> 24) & 0xFF);
            lengthBytes[1] = (byte)((dataLength >> 16) & 0xFF);
            lengthBytes[2] = (byte)((dataLength >> 8) & 0xFF);
            lengthBytes[3] = (byte)(dataLength & 0xFF);

            Log($"[Unity.Mcp] 发送消息: length={data.Length}, length_prefix={BitConverter.ToString(lengthBytes)}");

            try
            {
                await stream.WriteAsync(lengthBytes, 0, 4);
                await stream.WriteAsync(data, 0, data.Length);
            }
            catch (System.IO.IOException ex)
            {
                // 网络写入错误，通常表示连接已断开
                throw new ConnectionClosedException($"Connection closed during write: {ex.Message}");
            }
        }

        /// <summary>
        /// 接收带长度前缀的数据
        /// </summary>
        private static async Task<byte[]> ReceiveWithLengthAsync(NetworkStream stream)
        {
            // 读取4字节长度前缀
            byte[] lengthBytes = await ReadExactAsync(stream, 4);

            // 手动从大端序字节数组转换为长度值
            uint dataLength = ((uint)lengthBytes[0] << 24) |
                             ((uint)lengthBytes[1] << 16) |
                             ((uint)lengthBytes[2] << 8) |
                             ((uint)lengthBytes[3]);

            Log($"[Unity.Mcp] 接收长度前缀字节: {BitConverter.ToString(lengthBytes)} -> {dataLength} bytes");

            // 安全检查，防止内存问题
            const uint maxMessageSize = 100 * 1024 * 1024; // 100MB限制
            if (dataLength > maxMessageSize)
            {
                LogError($"[Unity.Mcp] 长度前缀字节详细: [{lengthBytes[0]}, {lengthBytes[1]}, {lengthBytes[2]}, {lengthBytes[3]}]");
                throw new Exception($"消息过大: {dataLength} bytes (最大: {maxMessageSize})");
            }

            if (dataLength == 0)
            {
                LogWarning($"[Unity.Mcp] 接收到长度为0的消息");
                return new byte[0];
            }

            // 读取指定长度的数据
            return await ReadExactAsync(stream, (int)dataLength);
        }

        private static async Task HandleClientAsync(TcpClient client)
        {
            string clientEndpoint = client.Client.RemoteEndPoint?.ToString() ?? "Unknown";
            string clientId = Guid.NewGuid().ToString();
            Log($"[Unity.Mcp] 客户端已连接: {clientEndpoint} (ID: {clientId})");

            // 添加客户端到连接列表
            var clientInfo = new ClientInfo
            {
                Id = clientId,
                EndPoint = clientEndpoint,
                ConnectedAt = DateTime.Now,
                LastActivity = DateTime.Now,
                CommandCount = 0
            };

            lock (clientsLock)
            {
                connectedClients[clientId] = clientInfo;
            }

            using (client)
            using (NetworkStream stream = client.GetStream())
            {
                while (isRunning)
                {
                    try
                    {
                        // 使用长度前缀协议接收数据
                        byte[] commandBytes = await ReceiveWithLengthAsync(stream);

                        string commandText = System.Text.Encoding.UTF8.GetString(commandBytes);
                        Log($"[Unity.Mcp] 接收到命令 from {clientEndpoint}: {commandText}");

                        // 更新客户端活动状态
                        lock (clientsLock)
                        {
                            if (connectedClients.TryGetValue(clientId, out var existingClient))
                            {
                                existingClient.LastActivity = DateTime.Now;
                                existingClient.CommandCount++;
                            }
                        }

                        string commandId = Guid.NewGuid().ToString();
                        TaskCompletionSource<string> tcs = new();

                        // Special handling for ping command to avoid Json parsing
                        if (commandText.Trim() == "ping")
                        {
                            Log($"[Unity.Mcp] 处理ping命令 from {clientEndpoint}");
                            // Direct response to ping without going through Json parsing
                            byte[] pingResponseBytes = System.Text.Encoding.UTF8.GetBytes(
                                /*lang=json,strict*/
                                "{\"status\":\"success\",\"result\":{\"message\":\"pong\"}}"
                            );
                            await SendWithLengthAsync(stream, pingResponseBytes);
                            Log($"[Unity.Mcp] ping响应已发送 to {clientEndpoint}");
                            continue;
                        }

                        lock (lockObj)
                        {
                            commandQueue[commandId] = (commandText, tcs);
                            Log($"[Unity.Mcp] 命令已加入队列 ID: {commandId}");
                        }

                        string response = await tcs.Task;
                        byte[] responseBytes = System.Text.Encoding.UTF8.GetBytes(response);
                        await SendWithLengthAsync(stream, responseBytes);
                        Log($"[Unity.Mcp] 响应已发送 to {clientEndpoint}, ID: {commandId}, Response: {response}");
                    }
                    catch (ConnectionClosedException ex)
                    {
                        // 正常的连接关闭，使用 Log 而不是 LogError
                        Log($"[Unity.Mcp] 客户端主动断开连接 {clientEndpoint}: {ex.Message}");
                        break;
                    }
                    catch (Exception ex)
                    {
                        // 真正的错误
                        LogError($"[Unity.Mcp] 客户端处理异常 {clientEndpoint}: {ex.Message}");
                        break;
                    }
                }
            }

            // 从连接列表中移除客户端
            lock (clientsLock)
            {
                connectedClients.Remove(clientId);
            }

            Log($"[Unity.Mcp] 客户端连接已关闭: {clientEndpoint} (ID: {clientId})");
        }

        private static void ProcessCommands()
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

        private static void ExecuteCommand(Command command, TaskCompletionSource<string> tcs)
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
                        if (command.type == "single_call")
                        {
                            // function_call: 记录具体的func和args
                            cmdName = "single_call." + paramsObject["func"]?.Value ?? "Unknown";
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

                    if (command?.type == "single_call" && command.cmd is JsonClass singleCallParams)
                    {
                        cmdName = "single_call." + (singleCallParams["func"]?.Value ?? "Unknown");
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
        private static McpTool GetMcpTool(string toolName)
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
}
