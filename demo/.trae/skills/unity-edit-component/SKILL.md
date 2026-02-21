---
name: unity-edit-component
description: 编辑 GameObject 上的组件属性。
---

# Unity MCP: edit_component

通过 `args` 传递参数。

## 参数 (args)

| 参数         | 类型     | 必填 | 说明 |
|-------------|----------|------|------|
| `action`   | string   | 是   | 操作: `set_property`, `get_property`, `list_components` |
| `target`   | string   | 是   | 目标 GameObject 路径 |
| `component`| string   | 是   | 组件类型名称 |
| `property` | string   | 否   | 属性名称 |
| `value`    | any      | 否   | 要设置的属性值 |

- **set_property** — 设置组件属性
- **get_property** — 获取组件属性
- **list_components** — 列出 GameObject 上的所有组件

## 示例参数

```json
{ "action": "set_property", "target": "Player", "component": "Rigidbody", "property": "mass", "value": 1.5 }
```

## 响应

成功: `{ "success": true, "data": ... }`. 失败: `{ "success": false, "message": "...", "error": "..." }`.
