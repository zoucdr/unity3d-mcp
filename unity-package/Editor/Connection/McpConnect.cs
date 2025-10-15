using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
// Migrated from Newtonsoft.Json to SimpleJson
using UnityEditor;
using UnityEngine;
using UnityMcp.Models;
using UnityMcp.Executer;

namespace UnityMcp
{
    [InitializeOnLoad]
    public static partial class McpConnect
    {
        private static TcpListener listener;
        private static bool isRunning = false;
        private static readonly object lockObj = new();
        private static Dictionary<
            string,
            (string commandJson, TaskCompletionSource<string> tcs)
        > commandQueue = new();
        public static readonly int unityPortStart = 6400; // Start of port range
        public static readonly int unityPortEnd = 6405;   // End of port range
        public static int currentPort = -1; // Currently used port
        public static bool IsRunning => isRunning;

        // Client connection state tracking
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

        // Client info class
        public class ClientInfo
        {
            public string Id { get; set; }
            public string EndPoint { get; set; }
            public DateTime ConnectedAt { get; set; }
            public DateTime LastActivity { get; set; }
            public int CommandCount { get; set; }
        }

        // CacheMcpToolType and instance，Static tool type
        private static readonly Dictionary<string, McpTool> mcpToolInstanceCache = new();
        //General function execution
        private static ToolsCall methodsCall = new ToolsCall();
        // At UnityMcp Add log switch in class
        public static bool EnableLog = false;

        // Unified log output method
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

        static McpConnect()
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
            // FromEditorPrefsRead log settings，Default tofalse
            McpConnect.EnableLog = EditorPrefs.GetBool("mcp_enable_log", false);
            Log($"[UnityMcp] StartingUnityMcp...");
            Stop();

            if (isRunning)
            {
                Log($"[UnityMcp] Service is already running");
                return;
            }

            // Try to start listener on available port in range
            bool started = false;
            SocketException lastException = null;

            for (int port = unityPortStart; port <= unityPortEnd; port++)
            {
                try
                {
                    Log($"[UnityMcp] Trying on port {port} StartTCPListener...");
                    listener = new TcpListener(IPAddress.Loopback, port);
                    listener.Start();
                    currentPort = port;
                    isRunning = true;
                    started = true;
                    Log($"[UnityMcp] TCPListener successfully started，Port: {port}");

                    // Start the listener loop and command processing
                    Task.Run(ListenerLoop);
                    EditorApplication.update += ProcessCommands;
                    Log($"[UnityMcp] Startup completed，Listener loop has started，Command handler registered");
                    break;
                }
                catch (SocketException ex)
                {
                    lastException = ex;
                    if (ex.SocketErrorCode == SocketError.AddressAlreadyInUse)
                    {
                        Log($"[UnityMcp] Port {port} Already in use，Try next port...");
                    }
                    else
                    {
                        LogWarning($"[UnityMcp] Port {port} Startup failed: {ex.Message}，Try next port...");
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
                LogError($"[UnityMcp] Cannot operate within port range {unityPortStart}-{unityPortEnd} Internal startupTCPListener。Last error: {lastException?.Message}");
                if (lastException?.SocketErrorCode == SocketError.AddressAlreadyInUse)
                {
                    LogError("[UnityMcp] All ports are occupied。Please ensure there are no othersUnity MCPInstance is running。");
                }
            }
        }

        public static void Stop()
        {
            if (!isRunning)
            {
                return;
            }

            Log($"[UnityMcp] StoppingUnityMcp...");

            try
            {
                listener?.Stop();
                listener = null;
                isRunning = false;
                currentPort = -1; // Reset current port

                // Clear client connection information
                lock (clientsLock)
                {
                    connectedClients.Clear();
                }

                EditorApplication.update -= ProcessCommands;
                Log($"[UnityMcp] Service stopped，TCPListener closed，Command handler unregistered");
            }
            catch (Exception ex)
            {
                LogError($"[UnityMcp] Error occurred while stopping the service: {ex.Message}");
            }
        }

        private static async Task ListenerLoop()
        {
            Log($"[UnityMcp] Listener loop started");

            while (isRunning)
            {
                try
                {
                    Log($"[UnityMcp] Waiting for client connection...");
                    TcpClient client = await listener.AcceptTcpClientAsync();

                    // Enable basic socket keepalive
                    client.Client.SetSocketOption(
                        SocketOptionLevel.Socket,
                        SocketOptionName.KeepAlive,
                        true
                    );

                    // Set longer receive timeout to prevent quick disconnections
                    client.ReceiveTimeout = 60000; // 60 seconds

                    Log($"[UnityMcp] Client connection configuration completed：KeepAlive=true, ReceiveTimeout=60s");

                    // Fire and forget each client connection
                    _ = HandleClientAsync(client);
                }
                catch (Exception ex)
                {
                    if (isRunning)
                    {
                        LogError($"[UnityMcp] Listener error: {ex.Message}");
                    }
                    else
                    {
                        Log($"[UnityMcp] Listener stopped");
                    }
                }
            }

            Log($"[UnityMcp] Listener loop ended");
        }

        /// <summary>
        /// Read a specified number of bytes from the stream
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
                    // Use a custom exception to indicate that this is a normal connection closure
                    throw new ConnectionClosedException($"Connection closed gracefully. Expected {count} bytes, received {totalBytesRead}");
                }
                totalBytesRead += bytesRead;
            }

            return buffer;
        }

        /// <summary>
        /// Connection closed unexpectedly（Closed normally，Not an error）
        /// </summary>
        private class ConnectionClosedException : Exception
        {
            public ConnectionClosedException(string message) : base(message) { }
        }

        /// <summary>
        /// Send data with length prefix
        /// </summary>
        private static async Task SendWithLengthAsync(NetworkStream stream, byte[] data)
        {
            uint dataLength = (uint)data.Length;

            // Manually construct a big-endian byte array
            byte[] lengthBytes = new byte[4];
            lengthBytes[0] = (byte)((dataLength >> 24) & 0xFF);
            lengthBytes[1] = (byte)((dataLength >> 16) & 0xFF);
            lengthBytes[2] = (byte)((dataLength >> 8) & 0xFF);
            lengthBytes[3] = (byte)(dataLength & 0xFF);

            Log($"[UnityMcp] Send message: length={data.Length}, length_prefix={BitConverter.ToString(lengthBytes)}");

            try
            {
                await stream.WriteAsync(lengthBytes, 0, 4);
                await stream.WriteAsync(data, 0, data.Length);
            }
            catch (System.IO.IOException ex)
            {
                // Network write error，Usually indicates that the connection is closed
                throw new ConnectionClosedException($"Connection closed during write: {ex.Message}");
            }
        }

        /// <summary>
        /// Receive data with length prefix
        /// </summary>
        private static async Task<byte[]> ReceiveWithLengthAsync(NetworkStream stream)
        {
            // Read4Byte length prefix
            byte[] lengthBytes = await ReadExactAsync(stream, 4);

            // Manually convert a big-endian byte array to a length value
            uint dataLength = ((uint)lengthBytes[0] << 24) |
                             ((uint)lengthBytes[1] << 16) |
                             ((uint)lengthBytes[2] << 8) |
                             ((uint)lengthBytes[3]);

            Log($"[UnityMcp] Receiving length-prefixed bytes: {BitConverter.ToString(lengthBytes)} -> {dataLength} bytes");

            // Security check，Prevent memory issues
            const uint maxMessageSize = 100 * 1024 * 1024; // 100MBLimit
            if (dataLength > maxMessageSize)
            {
                LogError($"[UnityMcp] Details of length-prefixed bytes: [{lengthBytes[0]}, {lengthBytes[1]}, {lengthBytes[2]}, {lengthBytes[3]}]");
                throw new Exception($"Message too large: {dataLength} bytes (Maximum: {maxMessageSize})");
            }

            if (dataLength == 0)
            {
                LogWarning($"[UnityMcp] Received length is0Message of");
                return new byte[0];
            }

            // Read data of specified length
            return await ReadExactAsync(stream, (int)dataLength);
        }

        private static async Task HandleClientAsync(TcpClient client)
        {
            string clientEndpoint = client.Client.RemoteEndPoint?.ToString() ?? "Unknown";
            string clientId = Guid.NewGuid().ToString();
            Log($"[UnityMcp] Client connected: {clientEndpoint} (ID: {clientId})");

            // Add client to connection list
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
                        // Receive data using length-prefixed protocol
                        byte[] commandBytes = await ReceiveWithLengthAsync(stream);

                        string commandText = System.Text.Encoding.UTF8.GetString(commandBytes);
                        Log($"[UnityMcp] Command received from {clientEndpoint}: {commandText}");

                        // Update client activity status
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
                            Log($"[UnityMcp] HandlepingCommand from {clientEndpoint}");
                            // Direct response to ping without going through Json parsing
                            byte[] pingResponseBytes = System.Text.Encoding.UTF8.GetBytes(
                                /*lang=json,strict*/
                                "{\"status\":\"success\",\"result\":{\"message\":\"pong\"}}"
                            );
                            await SendWithLengthAsync(stream, pingResponseBytes);
                            Log($"[UnityMcp] pingResponse sent to {clientEndpoint}");
                            continue;
                        }

                        lock (lockObj)
                        {
                            commandQueue[commandId] = (commandText, tcs);
                            Log($"[UnityMcp] Command added to queue ID: {commandId}");
                        }

                        string response = await tcs.Task;
                        byte[] responseBytes = System.Text.Encoding.UTF8.GetBytes(response);
                        await SendWithLengthAsync(stream, responseBytes);
                        Log($"[UnityMcp] Response sent to {clientEndpoint}, ID: {commandId}, Response: {response}");
                    }
                    catch (ConnectionClosedException ex)
                    {
                        // Normal connection closure，Use Log Instead of LogError
                        Log($"[UnityMcp] Client actively disconnected {clientEndpoint}: {ex.Message}");
                        break;
                    }
                    catch (Exception ex)
                    {
                        // Actual error
                        LogError($"[UnityMcp] Exception occurred while handling client {clientEndpoint}: {ex.Message}");
                        break;
                    }
                }
            }

            // Remove client from connection list
            lock (clientsLock)
            {
                connectedClients.Remove(clientId);
            }

            Log($"[UnityMcp] Client connection closed: {clientEndpoint} (ID: {clientId})");
        }

        private static void ProcessCommands()
        {
            List<string> processedIds = new();
            lock (lockObj)
            {
                if (commandQueue.Count > 0)
                {
                    Log($"[UnityMcp] Start processing command queue，Queue length: {commandQueue.Count}");
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

                    Log($"[UnityMcp] Processing command ID: {id}");

                    try
                    {
                        // Special case handling
                        if (string.IsNullOrEmpty(commandText))
                        {
                            LogWarning($"[UnityMcp] Received empty command ID: {id}");
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
                            Log($"[UnityMcp] HandlepingCommand ID: {id}");
                            var pingResponse = new
                            {
                                status = "success",
                                result = new { message = "pong" },
                            };
                            string pingResponseJson = Json.FromObject(pingResponse);
                            tcs.SetResult(pingResponseJson);
                            Log($"[UnityMcp] pingCommand processing finished ID: {id}");
                            processedIds.Add(id);
                            continue;
                        }

                        // Check if the command is valid Json before attempting to deserialize
                        if (!IsValidJson(commandText))
                        {
                            LogError($"[UnityMcp] InvalidJSONFormat ID: {id}, Content: {commandText}");
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
                        Log($"[UnityMcp] Start parsingJSONCommand ID: {id}");
                        Command command = DeserializeCommand(commandText);
                        if (command == null)
                        {
                            LogError($"[UnityMcp] Command deserialized asnull ID: {id}");
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
                            Log($"[UnityMcp] Execute command ID: {id}, Type: {command.type}");
                            // Execute command asynchronously，But do not wait for result，Run it in the background
                            try
                            {
                                ExecuteCommand(command, tcs);
                            }
                            catch (Exception asyncEx)
                            {
                                LogError($"[UnityMcp] Error occurred during asynchronous command execution ID: {id}: {asyncEx.Message}\n{asyncEx.StackTrace}");
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
                        LogError($"[UnityMcp] Error occurred while processing command ID: {id}: {ex.Message}\n{ex.StackTrace}");

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
                        Log($"[UnityMcp] Error response set ID: {id}");
                    }

                    processedIds.Add(id);
                }

                foreach (string id in processedIds)
                {
                    commandQueue.Remove(id);
                }

                if (processedIds.Count > 0)
                {
                    Log($"[UnityMcp] Command queue processing complete，Processed: {processedIds.Count} commands");
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
            Log($"[UnityMcp] Begin executing command: Type={command.type}");

            try
            {
                if (string.IsNullOrEmpty(command.type))
                {
                    LogError($"[UnityMcp] Command type is empty");
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
                    Log($"[UnityMcp] HandlepingCommand");
                    var pingResponse = new
                    {
                        status = "success",
                        result = new { message = "pong" },
                    };
                    Log($"[UnityMcp] pingCommand executed successfully");
                    tcs.SetResult(Json.FromObject(pingResponse));
                    return;
                }

                // Use JsonClass for args as the new handlers likely expect this
                JsonNode paramsObject = command.cmd ?? new JsonData("null");

                Log($"[UnityMcp] Command parameters: {paramsObject}");

                Log($"[UnityMcp] GetMcpToolInstance: {command.type}");
                var tool = GetMcpTool(command.type);
                if (tool == null)
                {
                    LogError($"[UnityMcp] Tool not found: {command.type}");
                    throw new ArgumentException($"Unknown or unsupported command type: {command.type}");
                }

                Log($"[UnityMcp] Tool found: {tool.GetType().Name}，Begin processing commands asynchronously");
                var startTime = System.DateTime.Now;
                tool.HandleCommand(paramsObject, (result) =>
                {
                    var endTime = System.DateTime.Now;
                    var duration = (endTime - startTime).TotalMilliseconds;

                    // Dynamically determine status：According to result In success Field
                    string status = "success";
                    string error = "";
                    if (result is JsonClass jsonResult)
                    {
                        var successNode = jsonResult["success"];
                        if (successNode != null && successNode.Value == "false")
                        {
                            status = "error";
                            error = jsonResult["error"].Value;
                        }
                    }

                    var response = new
                    {
                        status = status,
                        result = Json.FromObject(result)
                    };

                    // According to status Log different messages
                    if (status == "success")
                    {
                        Log($"[UnityMcp] Command executed successfully: Type={command.type} {command.cmd}");
                    }
                    else
                    {
                        Log($"[UnityMcp] Command execution failed: Type={command.type} {command.cmd}");
                    }

                    string re;
                    try
                    {
                        re = Json.FromObject((object)response).ToString();
                        Log($"[UnityMcp] Tool execution completed，Result: {re}");
                    }
                    catch (Exception serEx)
                    {
                        LogError($"[UnityMcp] Failed to serialize response: {serEx.Message}");
                        // Attempt to serialize a simplified error response
                        re = Json.FromObject((object)(new
                        {
                            status = "error",
                            error = $"Failed to serialize response: {serEx.Message}",
                            details = result?.GetType().ToString() ?? "null"
                        }));
                    }
                    // Logging execution result toMcpExecuteRecordObject
                    try
                    {
                        var recordObject = McpExecuteRecordObject.instance;

                        // Determine logging method based on command type
                        string cmdName;
                        string argsString;
                        if (command.type == "single_call")
                        {
                            // function_call: Logging detailsfuncAndargs
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
                            // Other command types: Use default mode
                            cmdName = command.type;
                            // Corrected to standardJSONFormat
                            argsString = Json.FromObject((object)(new { func = cmdName, args = paramsObject })).ToPrettyString();
                        }

                        recordObject.addRecord(
                            cmdName,
                            argsString,
                            re,
                            error, // On successerrorIs empty
                            duration,
                            "MCP Client"
                        );
                        recordObject.saveRecords();
                    }
                    catch (System.Exception recordEx)
                    {
                        LogError($"[UnityMcp] Error occurred while logging execution result: {recordEx.Message}");
                    }

                    tcs.SetResult(re);
                });
            }
            catch (Exception ex)
            {
                // Log the detailed error in Unity for debugging
                Debug.LogException(new Exception(
                    $"[UnityMcp] Error occurred while executing command '{command?.type ?? "Unknown"}': {ex.Message}\n{ex.StackTrace}",
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
                Log($"[UnityMcp] Error response generated: Type={command?.type ?? "Unknown"}");
                var errorResponse = Json.FromObject(response);

                // Log error execution result toMcpExecuteRecordObject
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
                        // batch_call RecordfuncName concatenation
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
                    LogError($"[UnityMcp] Error occurred while logging error execution result: {recordEx.Message}");
                }

                tcs.SetResult(errorResponse);
                return;
            }
        }
        /// <summary>
        /// GetMcpToolInstance
        /// </summary>
        /// <param name="toolName"></param>
        /// <returns></returns>
        private static McpTool GetMcpTool(string toolName)
        {
            Log($"[UnityMcp] Request to fetch tool: {toolName}");

            if (mcpToolInstanceCache.Count == 0)
            {
                Log($"[UnityMcp] Tool cache is empty，Start reflection to find tool instance");
                // Reflect to find and cache if not already cached
                var toolType = typeof(McpTool);
                var toolInstances = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => a.GetTypes())
                    .Where(t => !t.IsAbstract && toolType.IsAssignableFrom(t)).Select(t => Activator.CreateInstance(t) as McpTool);

                int cacheCount = 0;
                foreach (var toolInstance in toolInstances)
                {
                    mcpToolInstanceCache[toolInstance.ToolName] = toolInstance;
                    cacheCount++;
                    Log($"[UnityMcp] Cached tools: {toolInstance.ToolName} ({toolInstance.GetType().Name})");
                }
                Log($"[UnityMcp] Tool cache initialized，Total cached {cacheCount} tools");
            }

            if (mcpToolInstanceCache.TryGetValue(toolName, out var tool))
            {
                Log($"[UnityMcp] Retrieve tool from cache: {toolName} ({tool.GetType().Name})");
                return tool;
            }

            if (methodsCall.GetToolMethod(toolName) != null)
            {
                Log($"[UnityMcp] FrommethodsCallTool obtained from: {toolName}");
                methodsCall.SetToolName(toolName);
                return methodsCall;
            }

            LogError($"[UnityMcp] Tool not found: {toolName}，Available tools: [{string.Join(", ", mcpToolInstanceCache.Keys)}]");
            return null;
        }

        /// <summary>
        /// SimpleJson Deserialization Command Helper method
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
                Debug.LogError($"[UnityMcp] Deserialization Command Failure: {ex.Message}");
                return null;
            }
        }
    }
}
