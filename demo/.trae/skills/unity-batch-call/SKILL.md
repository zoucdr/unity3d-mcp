---
name: unity-batch-call
description: 批量执行多个函数调用。
---

# Unity MCP: batch_call

通过 `args` 传递参数。

## 参数 (args)

| 参数   | 类型   | 必填 | 说明 |
|--------|--------|------|------|
| `args` | array  | 是   | 要顺序执行的函数调用列表 |

数组中每个项目应包含:
| 参数   | 类型   | 必填 | 说明 |
|--------|--------|------|------|
| `func` | string | 是   | 要调用的函数名称 |
| `args` | object | 是   | 函数的参数 |

## 示例参数

```json
{
  "args": [
    { "func": "hierarchy_create", "args": { "name": "Object1", "source": "primitive", "primitive_type": "Cube" } },
    { "func": "hierarchy_create", "args": { "name": "Object2", "source": "primitive", "primitive_type": "Sphere" } }
  ]
}
```

## 响应

成功: `{ "success": true, "data": [ ... ] }`. 失败: `{ "success": false, "message": "...", "error": "..." }`.

批量中的每个函数将按顺序执行，结果以数组形式返回。
