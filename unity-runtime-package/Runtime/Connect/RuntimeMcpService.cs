using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace UniMcp.Runtime
{
    public class RuntimeMcpService : MonoBehaviour
    {
        private static RuntimeMcpService _instance;
        public static RuntimeMcpService Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("RuntimeMcpService");
                    _instance = go.AddComponent<RuntimeMcpService>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        [Header("MCP Server Settings")]
        [SerializeField] private int _serverPort = 8080;
        [SerializeField] private bool _autoStart = true;
        [SerializeField] private bool _enableLogging = true;

        public int ServerPort
        {
            get => _serverPort;
            set => _serverPort = value;
        }

        public bool AutoStart
        {
            get => _autoStart;
            set => _autoStart = value;
        }

        public bool EnableLogging
        {
            get => _enableLogging;
            set
            {
                _enableLogging = value;
                RuntimeLogger.EnableLog = value;
                RuntimeLogger.EnableWarning = value;
                RuntimeLogger.EnableError = value;
            }
        }

        private HttpListener listener;
        private bool isRunning = false;
        private CancellationTokenSource cancellationTokenSource;
        private Task listenerTask;

        private readonly Dictionary<string, IToolMethod> availableTools = new();
        private readonly Dictionary<string, ToolInfo> toolInfos = new();
        private readonly Dictionary<string, McpTool> mcpToolCache = new();
        private readonly Executer.ToolsCall toolsCall = new();

        private string serverName = "Unity Runtime MCP Server";
        private string serverVersion = "1.0.0";

        private readonly object lockObj = new object();

        public bool IsRunning => isRunning;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            RuntimeLogger.EnableLog = _enableLogging;
            RuntimeLogger.EnableWarning = _enableLogging;
            RuntimeLogger.EnableError = _enableLogging;

            DiscoverTools();
        }

        private void Start()
        {
            if (_autoStart)
            {
                StartService();
            }
        }

        private void OnDestroy()
        {
            StopService();
        }

        public void DiscoverTools()
        {
            RuntimeLogger.Log("[RuntimeMcp] Starting tool discovery...");

            availableTools.Clear();
            toolInfos.Clear();
            mcpToolCache.Clear();

            try
            {
                Executer.ToolsCall.EnsureMethodsRegisteredStatic();
                var registered = Executer.ToolsCall.GetRegisteredMethodNames();

                foreach (var methodName in registered)
                {
                    var method = Executer.ToolsCall.GetRegisteredMethod(methodName);
                    if (method != null)
                    {
                        availableTools[methodName] = method;
                        toolInfos[methodName] = CreateToolInfo(methodName, method);
                        RuntimeLogger.Log($"[RuntimeMcp] Registered tool: {methodName}");
                    }
                }

                RuntimeLogger.Log($"[RuntimeMcp] Tool discovery complete. Total tools: {availableTools.Count}");
            }
            catch (Exception ex)
            {
                RuntimeLogger.LogError($"[RuntimeMcp] Tool discovery failed: {ex.Message}");
            }
        }

        private ToolInfo CreateToolInfo(string toolName, IToolMethod toolInstance)
        {
            var toolInfo = new ToolInfo
            {
                name = toolName,
                description = !string.IsNullOrEmpty(toolInstance.Description) ? toolInstance.Description : $"Runtime tool: {toolName}"
            };

            if (toolInstance.Keys != null && toolInstance.Keys.Length > 0)
            {
                var properties = new JsonClass();
                var required = new JsonArray();

                foreach (var key in toolInstance.Keys)
                {
                    var property = new JsonClass();
                    string paramType = !string.IsNullOrEmpty(key.Type) ? key.Type : "string";
                    property.Add("type", new JsonData(paramType));
                    property.Add("description", new JsonData(key.Desc));

                    if (key is MethodVector methodVector)
                    {
                        property["type"] = new JsonData("array");
                        var items = new JsonClass();
                        items.Add("type", new JsonData("number"));
                        property.Add("items", items);
                        property.Add("minItems", new JsonData(methodVector.Dimension));
                        property.Add("maxItems", new JsonData(methodVector.Dimension));
                    }

                    if (key is MethodArr methodArr)
                    {
                        var items = new JsonClass();
                        items.Add("type", new JsonData(methodArr.ItemType));
                        property.Add("items", items);
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

        public void StartService()
        {
            if (isRunning)
            {
                RuntimeLogger.LogWarning("[RuntimeMcp] Service is already running");
                return;
            }

            RuntimeLogger.Log($"[RuntimeMcp] Starting MCP server on port {_serverPort}...");

            try
            {
                cancellationTokenSource?.Cancel();
                cancellationTokenSource?.Dispose();
                cancellationTokenSource = new CancellationTokenSource();

                listener = new HttpListener();
                string prefix127 = $"http://127.0.0.1:{_serverPort}/";
                string prefixLocalhost = $"http://localhost:{_serverPort}/";

                try
                {
                    listener.Prefixes.Add(prefix127);
                    listener.Prefixes.Add(prefixLocalhost);
                    listener.Start();
                    RuntimeLogger.Log($"[RuntimeMcp] Listener started on {prefix127} and {prefixLocalhost}");
                }
                catch (Exception)
                {
                    try
                    {
                        listener.Close();
                        listener = new HttpListener();
                        listener.Prefixes.Add(prefix127);
                        listener.Start();
                        RuntimeLogger.Log($"[RuntimeMcp] Listener started on {prefix127}");
                    }
                    catch (Exception ex)
                    {
                        RuntimeLogger.LogError($"[RuntimeMcp] Failed to start listener: {ex.Message}");
                        return;
                    }
                }

                isRunning = true;

                int port = _serverPort;
                listenerTask = Task.Run(async () =>
                {
                    try
                    {
                        await ListenerLoop(cancellationTokenSource.Token, port);
                    }
                    catch (Exception ex)
                    {
                        RuntimeLogger.LogError($"[RuntimeMcp] Listener loop error: {ex.Message}");
                        isRunning = false;
                    }
                });

                RuntimeLogger.Log($"[RuntimeMcp] <color=green>MCP server started successfully!</color>");
                RuntimeLogger.Log($"[RuntimeMcp] Available tools: {availableTools.Count}");
            }
            catch (Exception ex)
            {
                RuntimeLogger.LogError($"[RuntimeMcp] Failed to start server: {ex.Message}");
                isRunning = false;
            }
        }

        public void StopService()
        {
            if (!isRunning)
                return;

            RuntimeLogger.Log("[RuntimeMcp] Stopping MCP server...");

            try
            {
                cancellationTokenSource?.Cancel();

                if (listener != null && listener.IsListening)
                {
                    listener.Stop();
                    listener.Close();
                    listener = null;
                }

                if (listenerTask != null && !listenerTask.IsCompleted)
                {
                    try
                    {
                        Task.WaitAny(listenerTask, Task.Delay(500));
                    }
                    catch { }
                }

                isRunning = false;
                RuntimeLogger.Log("[RuntimeMcp] MCP server stopped");
            }
            catch (Exception ex)
            {
                RuntimeLogger.LogError($"[RuntimeMcp] Error stopping server: {ex.Message}");
            }
        }

        private async Task ListenerLoop(CancellationToken cancellationToken, int port)
        {
            RuntimeLogger.Log($"[RuntimeMcp] Listener loop started, port: {port}");

            while (isRunning && listener != null && listener.IsListening && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var context = await listener.GetContextAsync();
                    _ = HandleRequestAsync(context, cancellationToken, port);
                }
                catch (HttpListenerException)
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        RuntimeLogger.Log("[RuntimeMcp] Listener stopped");
                    }
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    RuntimeLogger.LogError($"[RuntimeMcp] Listener error: {ex.Message}");
                }
            }
        }

        private async Task HandleRequestAsync(HttpListenerContext context, CancellationToken cancellationToken, int port)
        {
            var request = context.Request;
            var response = context.Response;

            string clientEndpoint = request.RemoteEndPoint?.ToString() ?? "Unknown";
            RuntimeLogger.Log($"[RuntimeMcp] Received request: {request.HttpMethod} {request.Url} from {clientEndpoint}");

            try
            {
                response.Headers.Add("Access-Control-Allow-Origin", "*");
                response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
                response.Headers.Add("Access-Control-Allow-Headers", "Content-Type");
                response.ContentEncoding = Encoding.UTF8;

                if (request.HttpMethod == "OPTIONS")
                {
                    response.StatusCode = 200;
                    response.Close();
                    return;
                }

                if (request.HttpMethod == "GET")
                {
                    var serverInfo = new JsonClass();
                    serverInfo.Add("name", new JsonData(serverName));
                    serverInfo.Add("version", new JsonData(serverVersion));
                    serverInfo.Add("status", new JsonData("running"));
                    serverInfo.Add("port", new JsonData(port));
                    serverInfo.Add("toolCount", new JsonData(toolInfos.Count));
                    serverInfo.Add("protocol", new JsonData("MCP"));
                    serverInfo.Add("protocolVersion", new JsonData("2024-11-05"));

                    byte[] buffer = Encoding.UTF8.GetBytes(serverInfo.ToString());
                    response.ContentType = "application/json";
                    response.StatusCode = 200;
                    await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                    response.Close();
                    return;
                }

                if (request.HttpMethod != "POST")
                {
                    response.StatusCode = 405;
                    byte[] errorBytes = Encoding.UTF8.GetBytes("{\"jsonrpc\":\"2.0\",\"error\":{\"code\":-32601,\"message\":\"Method not allowed\"},\"id\":null}");
                    await response.OutputStream.WriteAsync(errorBytes, 0, errorBytes.Length);
                    response.Close();
                    return;
                }

                string requestBody = "";
                if (request.HasEntityBody)
                {
                    using (StreamReader reader = new StreamReader(request.InputStream, request.ContentEncoding))
                    {
                        requestBody = await reader.ReadToEndAsync();
                    }
                }

                if (string.IsNullOrWhiteSpace(requestBody))
                {
                    response.StatusCode = 400;
                    byte[] errorBytes = Encoding.UTF8.GetBytes("{\"jsonrpc\":\"2.0\",\"error\":{\"code\":-32600,\"message\":\"Empty request body\"},\"id\":null}");
                    await response.OutputStream.WriteAsync(errorBytes, 0, errorBytes.Length);
                    response.Close();
                    return;
                }

                RuntimeLogger.Log($"[RuntimeMcp] Request body: {requestBody}");
                string responseJson = await ProcessMcpRequest(requestBody);
                RuntimeLogger.Log($"[RuntimeMcp] Response: {responseJson}");

                byte[] responseBytes = Encoding.UTF8.GetBytes(responseJson);
                response.ContentType = "application/json";
                response.StatusCode = 200;
                await response.OutputStream.WriteAsync(responseBytes, 0, responseBytes.Length);
                response.Close();
            }
            catch (Exception ex)
            {
                RuntimeLogger.LogError($"[RuntimeMcp] Request handling error: {ex.Message}");

                try
                {
                    response.StatusCode = 500;
                    string errorMessage = ex.Message.Replace("\"", "\\\"").Replace("\n", "\\n");
                    string errorResponse = $"{{\"jsonrpc\":\"2.0\",\"error\":{{\"code\":-32603,\"message\":\"{errorMessage}\"}}}},\"id\":null}}";
                    byte[] errorBytes = Encoding.UTF8.GetBytes(errorResponse);
                    await response.OutputStream.WriteAsync(errorBytes, 0, errorBytes.Length);
                    response.Close();
                }
                catch { }
            }
        }

        private async Task<string> ProcessMcpRequest(string requestBody)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(requestBody))
                {
                    return CreateErrorResponse(null, -32600, "Invalid Request");
                }

                JsonNode requestJson = Json.Parse(requestBody);
                if (requestJson == null)
                {
                    return CreateErrorResponse(null, -32700, "Parse error");
                }

                RuntimeLogger.Log($"[RuntimeMcp] Parsed JSON: {requestJson}");
                
                var request = requestJson.AsObject;
                JsonNode methodNode = request["method"];
                JsonNode idNode = request["id"];
                JsonNode paramsNode = request["params"];
                
                string method = methodNode?.Value;
                JsonNode id = idNode;
                
                RuntimeLogger.Log($"[RuntimeMcp] Method node type: {methodNode?.type}, value: '{method}'");
                RuntimeLogger.Log($"[RuntimeMcp] Id node type: {idNode?.type}, value: '{idNode?.Value}'");

                if (string.IsNullOrEmpty(method))
                {
                    return CreateErrorResponse(id, -32600, "Invalid Request: method is missing or empty");
                }

                switch (method)
                {
                    case "initialize":
                        return HandleInitialize(id, paramsNode);

                    case "tools/list":
                        return HandleToolsList(id);

                    case "tools/call":
                        return await HandleToolsCall(id, paramsNode);

                    case "ping":
                        return HandlePing(id);

                    default:
                        return CreateErrorResponse(id, -32601, $"Method not found: {method}");
                }
            }
            catch (Exception ex)
            {
                RuntimeLogger.LogError($"[RuntimeMcp] Process request error: {ex.Message}");
                return CreateErrorResponse(null, -32603, $"Internal error: {ex.Message}");
            }
        }

        private string HandleInitialize(JsonNode id, JsonNode paramsNode)
        {
            RuntimeLogger.Log($"[RuntimeMcp] Handling initialize request");

            if (toolInfos.Count == 0)
            {
                DiscoverTools();
            }

            var result = new JsonClass();
            result.Add("protocolVersion", new JsonData("2024-11-05"));

            var capabilities = new JsonClass();
            var toolsCapability = new JsonClass();
            toolsCapability.Add("listChanged", new JsonData(false));
            capabilities.Add("tools", toolsCapability);

            var resourcesCapability = new JsonClass();
            resourcesCapability.Add("listChanged", new JsonData(false));
            resourcesCapability.Add("subscribe", new JsonData(false));
            capabilities.Add("resources", resourcesCapability);

            result.Add("capabilities", capabilities);

            var serverInfo = new JsonClass();
            serverInfo.Add("name", new JsonData(serverName));
            serverInfo.Add("version", new JsonData(serverVersion));
            result.Add("serverInfo", serverInfo);

            return CreateSuccessResponse(id, result);
        }

        private string HandleToolsList(JsonNode id)
        {
            RuntimeLogger.Log($"[RuntimeMcp] Handling tools/list request, count: {toolInfos.Count}");

            if (toolInfos.Count == 0)
            {
                DiscoverTools();
            }

            var tools = new JsonArray();
            foreach (var toolInfo in toolInfos.Values)
            {
                var tool = new JsonClass();
                tool.Add("name", new JsonData(toolInfo.name));
                tool.Add("description", new JsonData(toolInfo.description));

                if (toolInfo.inputSchema != null)
                {
                    tool.Add("inputSchema", toolInfo.inputSchema);
                }

                tools.Add(tool);
            }

            var result = new JsonClass();
            result.Add("tools", tools);

            return CreateSuccessResponse(id, result);
        }

        private async Task<string> HandleToolsCall(JsonNode id, JsonNode paramsNode)
        {
            RuntimeLogger.Log($"[RuntimeMcp] Handling tools/call request");

            try
            {
                if (paramsNode == null)
                {
                    return CreateErrorResponse(id, -32600, "Invalid params");
                }

                string toolName = paramsNode["name"]?.Value;
                JsonNode args = paramsNode["arguments"];

                if (string.IsNullOrEmpty(toolName))
                {
                    return CreateErrorResponse(id, -32600, "Missing tool name");
                }

                RuntimeLogger.Log($"[RuntimeMcp] Calling tool: {toolName}");

                if (!availableTools.TryGetValue(toolName, out var toolMethod))
                {
                    return CreateErrorResponse(id, -32601, $"Tool not found: {toolName}");
                }

                string resultJson = null;
                var tcs = new TaskCompletionSource<string>();

                try
                {
                    var state = new StateTreeContext(args?.AsObject ?? new JsonClass(), new Dictionary<string, object>());

                    state.RegistComplete((result) =>
                    {
                        try
                        {
                            var response = new JsonClass();
                            var content = new JsonClass();
                            content.Add("success", new JsonData(true));
                            content.Add("result", result);
                            response.Add("content", content);
                            tcs.TrySetResult(CreateSuccessResponse(id, response));
                        }
                        catch (Exception ex)
                        {
                            tcs.TrySetResult(CreateErrorResponse(id, -32603, $"Callback error: {ex.Message}"));
                        }
                    });

                    toolMethod.ExecuteMethod(state);

                    if (state.Result != null && tcs.Task.IsCompleted)
                    {
                        return tcs.Task.Result;
                    }

                    resultJson = await Task.Run(() => tcs.Task.Wait(5000) ? tcs.Task.Result : CreateErrorResponse(id, -32000, "Tool execution timed out"));

                    return resultJson;
                }
                catch (TimeoutException)
                {
                    return CreateErrorResponse(id, -32000, "Tool execution timed out");
                }
            }
            catch (Exception ex)
            {
                RuntimeLogger.LogError($"[RuntimeMcp] Tool call error: {ex.Message}");
                return CreateErrorResponse(id, -32603, $"Tool execution error: {ex.Message}");
            }
        }

        private string HandlePing(JsonNode id)
        {
            var result = new JsonClass();
            result.Add("status", new JsonData("pong"));
            return CreateSuccessResponse(id, result);
        }

        private string CreateSuccessResponse(JsonNode id, JsonNode result)
        {
            var response = new JsonClass();
            response.Add("jsonrpc", new JsonData("2.0"));
            response.Add("result", result);

            if (id != null)
            {
                response.Add("id", id);
            }
            else
            {
                response.Add("id", JsonNull.Instance);
            }

            return response.ToString();
        }

        private string CreateErrorResponse(JsonNode id, int code, string message)
        {
            var response = new JsonClass();
            response.Add("jsonrpc", new JsonData("2.0"));

            var error = new JsonClass();
            error.Add("code", new JsonData(code));
            error.Add("message", new JsonData(message));
            response.Add("error", error);

            if (id != null)
            {
                response.Add("id", id);
            }
            else
            {
                response.Add("id", JsonNull.Instance);
            }

            return response.ToString();
        }

        public static void RegisterTool(string toolName, IToolMethod toolMethod)
        {
            if (Instance != null)
            {
                Instance.availableTools[toolName] = toolMethod;
                Instance.toolInfos[toolName] = Instance.CreateToolInfo(toolName, toolMethod);
                RuntimeLogger.Log($"[RuntimeMcp] Manually registered tool: {toolName}");
            }
        }

        public int GetToolCount()
        {
            return availableTools.Count;
        }

        public string[] GetToolNames()
        {
            return availableTools.Keys.ToArray();
        }
    }

    public class JsonNull : JsonNode
    {
        public static readonly JsonNull Instance = new JsonNull();

        public override string Value
        {
            get { return "null"; }
            set { }
        }

        public override bool IsNull()
        {
            return true;
        }

        public override string ToString()
        {
            return "null";
        }
    }
}
