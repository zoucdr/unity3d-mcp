# Unity MCP Service

基于 Go 语言和 Vue 前端的 MCP (Model Context Protocol) 服务系统，参考 Unity Editor 中的 MCP 服务实现。

## 项目结构

```
service/
├── cmd/
│   └── server/
│       └── main.go          # 服务入口
├── internal/
│   ├── mcp/                # MCP 服务核心
│   │   ├── service.go      # MCP 服务实现
│   │   ├── tool.go         # 工具接口和注册
│   │   ├── execution.go    # 执行上下文和结果
│   │   └── example_tool.go # 示例工具
│   ├── models/             # 数据模型
│   │   └── response.go     # 响应模型
│   └── statetree/          # 状态树系统
│       ├── statetree.go    # 状态树核心
│       ├── context.go      # 上下文管理
│       └── builder.go      # 状态树构建器
├── frontend/               # Vue 前端
│   ├── src/
│   │   ├── api/           # API 调用
│   │   ├── stores/        # Pinia 状态管理
│   │   ├── views/         # 页面组件
│   │   ├── router/        # 路由配置
│   │   ├── App.vue        # 根组件
│   │   └── main.js        # 入口文件
│   ├── index.html
│   ├── package.json
│   └── vite.config.js
└── go.mod                 # Go 模块配置
```

## 功能特性

### 后端 (Go)
- MCP 服务核心框架
- 工具注册和管理
- 状态树系统
- RESTful API 接口
- 工具执行引擎

### 前端 (Vue)
- 工具列表展示
- 工具执行界面
- 实时结果展示
- 响应式设计

## 快速开始

### 后端启动

```bash
cd service
go run cmd/server/main.go
```

服务将在 `http://localhost:8080` 启动。

### 前端启动

```bash
cd service/frontend
npm install
npm run dev
```

前端将在 `http://localhost:3000` 启动。

## API 接口

### 健康检查
```
GET /health
```

### 服务器信息
```
GET /server/info
```

### 获取工具列表
```
GET /tools
```

### 执行工具
```
POST /tools/:name/execute
Content-Type: application/json

{
  "param1": "value1",
  "param2": "value2"
}
```

### 获取工具 Schema
```
GET /tools/:name/schema
```

## 开发工具

### 注册新工具

1. 创建工具结构体，实现 `Tool` 接口：

```go
type MyTool struct{}

func (mt *MyTool) Name() string {
    return "my_tool"
}

func (mt *MyTool) Description() string {
    return "My tool description"
}

func (mt *MyTool) InputSchema() map[string]interface{} {
    return map[string]interface{}{
        "type": "object",
        "properties": map[string]interface{}{
            "param": map[string]interface{}{
                "type": "string",
                "description": "Parameter description",
            },
        },
        "required": []string{"param"},
    }
}

func (mt *MyTool) Execute(ctx *mcp.ExecutionContext) (*mcp.ExecutionResult, error) {
    // 实现工具逻辑
    return &mcp.ExecutionResult{
        ToolName: mt.Name(),
        Success:  true,
        Message:  "Tool executed successfully",
        Data:     nil,
    }, nil
}
```

2. 在服务启动时注册工具：

```go
service := mcp.NewMcpService(config)
service.GetToolRegistry().Register(&MyTool{})
service.Start()
```

### 使用状态树

```go
import "unity-mcp-service/internal/statetree"

// 创建状态树
tree := statetree.Create().
    Key("action").
    Leaf("create", func(ctx *statetree.Context) interface{} {
        return "Creating..."
    }).
    Leaf("update", func(ctx *statetree.Context) interface{} {
        return "Updating..."
    }).
    Build()

// 执行状态树
ctx := statetree.NewContext()
ctx.SetJsonData(map[string]interface{}{
    "action": "create",
})
result := tree.Run(ctx)
```

## 技术栈

### 后端
- Go 1.21
- Gin (Web 框架)
- Logrus (日志)

### 前端
- Vue 3
- Vite
- Vue Router
- Pinia
- Axios

## 参考

- [Unity MCP Service (C#)](/Users/zht/WorkSpace/unity3d-mcp/unity-package/Editor/Connect/McpService.cs)
- [StateTree System (C#)](/Users/zht/WorkSpace/unity3d-mcp/unity-package/Editor/StateTree/StateTree.cs)
