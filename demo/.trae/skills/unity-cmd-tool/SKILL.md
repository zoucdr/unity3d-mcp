---
name: unity-cmd-tool
description: 执行系统 CMD 命令。
---

# Unity MCP: cmd_tool

通过 `args` 传递参数。

## 参数 (args)

| 参数          | 类型     | 必填 | 说明 |
|--------------|----------|------|------|
| `cmd`        | string   | 是   | 要执行的 CMD 命令 |
| `args`       | array    | 否   | 命令参数 |
| `env`        | object   | 否   | 环境变量 |
| `timeout`    | integer  | 否   | 超时时间(秒，默认: 30) |
| `working_dir`| string   | 否   | 工作目录 |

## 示例参数

```json
{ "cmd": "ls", "args": ["-la"], "working_dir": "/Users/zht/WorkSpace/project" }
```

## 响应

成功: `{ "success": true, "data": ... }`. 失败: `{ "success": false, "message": "...", "error": "..." }`.
