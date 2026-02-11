# Unity MCP HTTP 服务器

## 概述

Unity MCP HTTP 服务器是一个纯HTTP实现的MCP（Model Context Protocol）服务器，直接集成在Unity编辑器中。它允许客户端（如Cursor）通过简单的HTTP连接直接与Unity进行交互，无需额外的Python服务器。

## 主要特性

- **纯HTTP实现**：无需Python服务器，直接在Unity编辑器中运行
- **自动工具发现**：通过反射自动发现所有继承自`StateMethodBase`的工具
- **标准MCP协议**：完全兼容MCP协议规范
- **简单配置**：客户端只需配置一个HTTP地址即可

## 配置

### Unity编辑器配置

1. 确保Unity项目中已导入MCP包
2. 在Unity编辑器中，服务器会自动启动（如果在EditorPrefs中启用）
3. 默认监听端口：`8010`
4. 可通过Unity菜单或MCP状态窗口控制服务器启停

### 客户端配置（Cursor）

在Cursor的MCP配置文件中（通常是`~/.cursor/mcp.json`），添加或更新unityMCP配置：

```json
{
  "mcpServers": {
    "unityMCP": {
      "url": "http://127.0.0.1:8010",
      "headers": {},
      "type": "http"
    }
  }
}
```

## 支持的HTTP方法

### GET /
返回服务器状态信息，用于健康检查和服务发现。

**响应示例：**
```json
{
  "name": "Unity MCP Server",
  "version": "1.0.0",
  "status": "running",
  "port": 8010,
  "toolCount": 25,
  "protocol": "MCP",
  "protocolVersion": "2024-11-05"
}
```

### POST / (MCP协议)

#### initialize
初始化MCP连接，返回服务器信息和能力。

#### tools/list
列出所有可用的Unity工具。工具信息通过反射从`StateMethodBase`实例自动提取。

#### tools/call
调用指定的Unity工具。

**参数格式：**
```json
{
  "name": "工具名称",
  "arguments": {
    "参数名": "参数值"
  }
}
```

## 工具发现机制

服务器通过以下方式自动发现工具：

1. **反射扫描**：扫描所有程序集，查找实现`IToolMethod`接口的类
2. **工具名称**：优先使用`ToolNameAttribute`指定的名称，否则将类名转换为snake_case格式
3. **参数模式**：从工具的`Keys`属性自动生成JSON Schema

## 示例工具

### base_editor
系统管理工具，提供编辑器状态查询功能。

**可用操作：**
- `get_state`：获取编辑器状态
- `get_windows`：获取编辑器窗口信息
- `get_selection`：获取当前选择的对象

**使用示例：**
```json
{
  "name": "base_editor",
  "arguments": {
    "action": "get_state"
  }
}
```

## 开发指南

### 创建新工具

1. 继承`StateMethodBase`或`DualStateMethodBase`
2. 实现`CreateKeys()`方法定义参数
3. 实现`CreateStateTree()`方法定义逻辑
4. 可选：添加`ToolNameAttribute`指定工具名称

```csharp
[ToolName("my_tool", "我的工具组")]
public class MyTool : StateMethodBase
{
    protected override MethodKey[] CreateKeys()
    {
        return new[]
        {
            new MethodKey("action", "操作类型", false),
            new MethodKey("target", "目标对象", true)
        };
    }

    protected override StateTree CreateStateTree()
    {
        return StateTreeBuilder
            .Create()
            .Key("action")
                .Leaf("do_something", HandleDoSomething)
            .Build();
    }

    private object HandleDoSomething(JsonClass args)
    {
        // 实现具体逻辑
        return Response.Success("操作完成");
    }
}
```

## 调试和监控

### Unity编辑器
- 使用MCP调试窗口查看连接状态
- 查看Unity Console获取详细日志
- MCP状态窗口显示服务器运行状态

### 网络调试
- 服务器监听地址：`http://127.0.0.1:8010`
- 可使用Postman等工具直接测试HTTP请求
- 支持CORS，允许跨域访问

## 故障排除

### 服务器无法启动
1. 检查端口8010是否被占用
2. 确保Unity以管理员权限运行（Windows）
3. 检查防火墙设置

### 工具未被发现
1. 确保工具类实现了`IToolMethod`接口
2. 检查程序集是否正确加载
3. 查看Unity Console中的工具发现日志

### 连接失败
1. 确认Unity MCP服务器正在运行
2. 检查客户端配置的URL是否正确
3. 验证网络连接和防火墙设置

## 性能考虑

- 工具发现在服务启动时进行，运行时无额外开销
- HTTP请求处理是异步的，不会阻塞Unity主线程
- 大量并发请求时可能需要调整Unity的线程池设置

## 安全注意事项

- 服务器仅监听本地地址（127.0.0.1）
- 不建议在生产环境中暴露到公网
- 工具执行权限与Unity编辑器相同，需谨慎使用

## 版本兼容性

- Unity 2021.3 LTS 及以上版本
- .NET Framework 4.8 或 .NET Standard 2.1
- MCP协议版本：2024-11-05
