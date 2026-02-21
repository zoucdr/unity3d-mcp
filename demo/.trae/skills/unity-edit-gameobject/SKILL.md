---
name: unity-edit-gameobject
description: 编辑 GameObject 属性。
---

# Unity MCP: edit_gameobject

通过 `args` 传递参数。

## 参数 (args)

| 参数         | 类型     | 必填 | 说明 |
|-------------|----------|------|------|
| `action`   | string   | 是   | 操作: `set_property`, `get_property`, `add_component`, `remove_component`, `set_transform` |
| `target`   | string   | 是   | 目标 GameObject 路径或名称 |
| `property` | string   | 否   | 要设置/获取的属性名称 |
| `value`    | any      | 否   | 属性值 |
| `component`| string   | 否   | 要添加/删除的组件类型 |
| `position` | array    | 否   | 变换位置 [x, y, z] |
| `rotation` | array    | 否   | 变换旋转 [x, y, z] |
| `scale`    | array    | 否   | 变换缩放 [x, y, z] |

- **set_property** — 设置 GameObject 属性
- **get_property** — 获取 GameObject 属性
- **add_component** — 添加组件到 GameObject
- **remove_component** — 从 GameObject 移除组件
- **set_transform** — 设置变换属性

## 示例参数

```json
{ "action": "set_property", "target": "Player", "property": "active", "value": true }
```

## 响应

成功: `{ "success": true, "data": ... }`. 失败: `{ "success": false, "message": "...", "error": "..." }`.
