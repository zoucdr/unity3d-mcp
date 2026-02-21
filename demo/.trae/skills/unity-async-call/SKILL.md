---
name: unity-async-call
description: Unity 异步函数调用支持。
---

# Unity MCP: async_call

通过 `args` 传递参数。

## 参数 (args)

| 参数    | 类型    | 必填 | 说明 |
|---------|---------|------|------|
| `id`    | string  | 是   | 异步调用的唯一标识符 |
| `type`  | string  | 是   | 操作类型: `in` (开始调用), `out` (获取结果) |
| `func`  | string  | 否   | 函数名称 (type 为 `in` 时必填) |
| `args`  | object  | 否   | 函数参数 (type 为 `in` 时必填) |

- **type: `in`** — 发起异步函数调用，需要 `func` 和 `args`
- **type: `out`** — 获取之前异步调用的结果，需要 `id`

## 示例参数

发起异步调用:
```json
{ "id": "call_001", "type": "in", "func": "SomeFunction", "args": { "param1": "value1" } }
```

获取结果:
```json
{ "id": "call_001", "type": "out" }
```

## 响应

成功: `{ "success": true, "data": ... }`. 失败: `{ "success": false, "message": "...", "error": "..." }`.
