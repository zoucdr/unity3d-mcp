---
name: unity-code-runner
description: 在 Unity 中运行时执行 C# 代码。
---

# Unity MCP: code_runner

通过 `args` 传递参数。

## 参数 (args)

| 参数           | 类型     | 必填 | 说明 |
|---------------|----------|------|------|
| `action`     | string   | 是   | 操作: `run`, `compile`, `eval` |
| `code`       | string   | 是   | 要执行的 C# 代码 |
| `context`   | object   | 否   | 执行上下文/变量 |
| `timeout`   | integer  | 否   | 超时时间(秒，默认: 30) |
| `assembly_name`| string   | 否   | 目标程序集名称 |

- **run** — 编译并运行 C# 代码
- **compile** — 编译代码但不运行
- **eval** — 快速求值表达式

## 示例参数

```json
{ "action": "run", "code": "Debug.Log(\"Hello from Unity!\");" }
```

## 响应

成功: `{ "success": true, "data": ... }`. 失败: `{ "success": false, "message": "...", "error": "..." }`.
