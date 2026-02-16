# Unity MCP HTTP 路由注册功能

## 概述

Unity MCP 服务器现在支持注册自定义 HTTP 路由，允许你复用 MCP 服务器的端口来处理自定义的 HTTP 请求。这意味着你可以：

- 在同一个端口上同时运行 MCP 协议和自定义 HTTP API
- 支持任意 HTTP 方法（GET、POST、PUT、DELETE、PATCH 等）
- 支持路径参数和通配符匹配
- 无需额外占用端口

## 快速开始

### 1. 基本用法

```csharp
using UniMcp;

// 注册一个简单的 GET 路由
McpService.Instance.RegistHttpRequest("/api/test", "GET", async (ctx) =>
{
    return "{\"message\": \"Hello World\"}";
});
```

### 2. 访问路由

启动 MCP 服务器后，你可以通过以下方式访问：

```bash
# 使用 127.0.0.1
curl http://127.0.0.1:8000/api/test

# 使用 localhost
curl http://localhost:8000/api/test

# 使用任意路径前缀
curl http://localhost:8000/mcp/api/test
```

## API 参考

### RegistHttpRequest

注册自定义 HTTP 路由处理器。

```csharp
public void RegistHttpRequest(
    string path,              // 路径模式
    string method,            // HTTP 方法
    HttpRouteHandler handler, // 处理器委托
    int priority = 0          // 优先级（可选）
)
```

**参数说明：**

- `path`: 路径模式，支持以下格式：
  - 精确匹配：`/api/test`
  - 路径参数：`/user/{id}` 或 `/api/{version}/users`
  - 通配符：`/files/*` 匹配 `/files/` 下的所有路径
  
- `method`: HTTP 方法
  - 具体方法：`GET`、`POST`、`PUT`、`DELETE`、`PATCH` 等
  - 通配符：`*` 表示匹配所有方法
  
- `handler`: 异步处理器函数，签名为：
  ```csharp
  async Task<string> Handler(HttpRequestContext context)
  ```
  
- `priority`: 优先级（默认 0），值越大越优先匹配

**返回值：**

- 无返回值（void）

### UnregistHttpRequest

注销指定路径的 HTTP 路由。

```csharp
public int UnregistHttpRequest(
    string path,         // 路径模式
    string method = null // HTTP 方法（可选）
)
```

**参数说明：**

- `path`: 要注销的路径模式
- `method`: HTTP 方法，为 `null` 或 `*` 时注销该路径的所有方法

**返回值：**

- 注销的路由数量（int）

### GetRegisteredRoutes

获取所有已注册的路由信息。

```csharp
public List<string> GetRegisteredRoutes()
```

**返回值：**

- 路由信息列表，每个元素格式为：`"{METHOD} {PATH} (优先级: {PRIORITY})"`

### ClearCustomRoutes

清空所有自定义路由。

```csharp
public void ClearCustomRoutes()
```

## HttpRequestContext

处理器函数接收的上下文对象，包含以下属性：

```csharp
public class HttpRequestContext
{
    public HttpListenerRequest Request { get; set; }      // 原始 HTTP 请求
    public HttpListenerResponse Response { get; set; }    // 原始 HTTP 响应
    public string Path { get; set; }                      // 请求路径
    public string Method { get; set; }                    // HTTP 方法
    public string Body { get; set; }                      // 请求体内容
    public Dictionary<string, string> QueryParams { get; set; }  // 查询参数
    public Dictionary<string, string> PathParams { get; set; }   // 路径参数
}
```

## 使用示例

### 示例 1: 简单的 JSON API

```csharp
McpService.Instance.RegistHttpRequest("/api/status", "GET", async (ctx) =>
{
    return "{\"status\": \"ok\", \"uptime\": " + Time.realtimeSinceStartup + "}";
});
```

测试：
```bash
curl http://localhost:8000/api/status
# 输出: {"status": "ok", "uptime": 123.45}
```

### 示例 2: POST 请求处理

```csharp
McpService.Instance.RegistHttpRequest("/api/echo", "POST", async (ctx) =>
{
    Debug.Log($"收到数据: {ctx.Body}");
    return "{\"received\": " + ctx.Body + "}";
});
```

测试：
```bash
curl -X POST http://localhost:8000/api/echo \
  -H "Content-Type: application/json" \
  -d '{"message":"test"}'
# 输出: {"received": {"message":"test"}}
```

### 示例 3: 路径参数

```csharp
McpService.Instance.RegistHttpRequest("/api/user/{id}", "GET", async (ctx) =>
{
    string userId = ctx.PathParams["id"];
    Debug.Log($"获取用户 ID: {userId}");
    return "{\"userId\": \"" + userId + "\", \"name\": \"User " + userId + "\"}";
});
```

测试：
```bash
curl http://localhost:8000/api/user/123
# 输出: {"userId": "123", "name": "User 123"}
```

### 示例 4: 查询参数

```csharp
McpService.Instance.RegistHttpRequest("/api/search", "GET", async (ctx) =>
{
    string query = ctx.QueryParams.ContainsKey("q") ? ctx.QueryParams["q"] : "";
    string page = ctx.QueryParams.ContainsKey("page") ? ctx.QueryParams["page"] : "1";
    
    Debug.Log($"搜索: {query}, 页码: {page}");
    return "{\"query\": \"" + query + "\", \"page\": " + page + "}";
});
```

测试：
```bash
curl "http://localhost:8000/api/search?q=unity&page=2"
# 输出: {"query": "unity", "page": 2}
```

### 示例 5: 通配符路由

```csharp
McpService.Instance.RegistHttpRequest("/files/*", "GET", async (ctx) =>
{
    Debug.Log($"文件请求: {ctx.Path}");
    
    // 提取文件路径
    string filePath = ctx.Path.Substring("/files/".Length);
    return "{\"file\": \"" + filePath + "\", \"exists\": false}";
});
```

测试：
```bash
curl http://localhost:8000/files/documents/test.txt
# 输出: {"file": "documents/test.txt", "exists": false}
```

### 示例 6: 返回 HTML

```csharp
McpService.Instance.RegistHttpRequest("/dashboard", "GET", async (ctx) =>
{
    // 修改 Content-Type
    ctx.Response.ContentType = "text/html; charset=utf-8";
    
    return @"
    <!DOCTYPE html>
    <html>
    <head><title>Unity Dashboard</title></head>
    <body>
        <h1>Unity MCP Dashboard</h1>
        <p>Server is running!</p>
    </body>
    </html>";
});
```

### 示例 7: 支持多种方法

```csharp
McpService.Instance.RegistHttpRequest("/api/resource", "*", async (ctx) =>
{
    switch (ctx.Method)
    {
        case "GET":
            return "{\"action\": \"read\"}";
        case "POST":
            return "{\"action\": \"create\"}";
        case "PUT":
            return "{\"action\": \"update\"}";
        case "DELETE":
            return "{\"action\": \"delete\"}";
        default:
            return "{\"action\": \"unknown\"}";
    }
});
```

### 示例 8: 复杂的路径参数

```csharp
McpService.Instance.RegistHttpRequest("/api/{version}/users/{userId}", "GET", async (ctx) =>
{
    string version = ctx.PathParams["version"];
    string userId = ctx.PathParams["userId"];
    
    return "{\"api_version\": \"" + version + "\", \"user_id\": \"" + userId + "\"}";
});
```

测试：
```bash
curl http://localhost:8000/api/v1/users/42
# 输出: {"api_version": "v1", "user_id": "42"}
```

### 示例 9: 高优先级路由

```csharp
// 这个路由会优先于通配符路由匹配
McpService.Instance.RegistHttpRequest("/api/special", "GET", async (ctx) =>
{
    return "{\"type\": \"special\"}";
}, priority: 100);

// 通配符路由
McpService.Instance.RegistHttpRequest("/api/*", "GET", async (ctx) =>
{
    return "{\"type\": \"general\"}";
}, priority: 0);
```

## 路由匹配规则

1. **优先级排序**：路由按优先级从高到低匹配，优先级相同时按注册顺序
2. **方法匹配**：先检查 HTTP 方法是否匹配（`*` 匹配所有方法）
3. **路径匹配**：
   - 精确匹配优先级最高
   - 路径参数匹配次之
   - 通配符匹配优先级最低

## 注意事项

1. **MCP 协议优先**：自定义路由在 MCP 协议处理之前匹配，但不会影响 MCP 的正常工作
2. **返回值**：处理器必须返回字符串（响应内容），返回 `null` 表示不处理该请求
3. **异常处理**：处理器中的异常会被自动捕获并记录，不会导致服务器崩溃
4. **线程安全**：路由注册和注销是线程安全的
5. **Content-Type**：默认为 `application/json`，可通过 `ctx.Response.ContentType` 修改

## 完整示例

参考 `unity-package/Editor/Examples/CustomHttpRouteExample.cs` 查看完整的示例代码。

可通过 Unity 菜单访问：
- `UniMcp/Examples/Register Custom Routes` - 注册示例路由
- `UniMcp/Examples/Unregister Custom Routes` - 注销示例路由
- `UniMcp/Examples/Clear All Custom Routes` - 清空所有路由
- `UniMcp/Examples/List Custom Routes` - 列出所有路由

## 常见问题

### Q: 自定义路由会影响 MCP 功能吗？
A: 不会。自定义路由在 MCP 协议处理之前匹配，只有未匹配的请求才会进入 MCP 处理流程。

### Q: 可以注册多少个路由？
A: 理论上无限制，但建议控制在合理数量（如 100 个以内）以保证性能。

### Q: 路由注册后多久生效？
A: 立即生效，无需重启服务器。

### Q: 如何调试路由？
A: 可以在处理器中使用 `Debug.Log` 输出日志，或在 Unity Console 中查看 MCP 服务的日志输出。

### Q: 支持异步操作吗？
A: 支持。处理器是异步函数，可以使用 `await` 调用其他异步 API。

## 更新日志

### 2026-02-16
- 首次发布 HTTP 路由注册功能
- 支持路径参数、通配符、优先级
- 支持所有 HTTP 方法
- 提供完整的示例代码
