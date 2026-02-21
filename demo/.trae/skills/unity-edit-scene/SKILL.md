---
name: unity-edit-scene
description: 管理场景资源。
---

# Unity MCP: edit_scene

通过 `args` 传递参数。

## 参数 (args)

| 参数          | 类型    | 必填 | 说明 |
|--------------|---------|------|------|
| `action`     | string  | 是   | 操作: `load`, `save`, `create`, `get_hierarchy` |
| `name`       | string  | 否   | 场景名称 |
| `path`       | string  | 否   | 场景资源路径 |
| `build_index`| integer | 否   | 构建索引 |

- **load** — 加载场景
- **save** — 保存当前场景
- **create** — 创建新场景
- **get_hierarchy** — 获取场景层级

## 示例参数

加载场景:
```json
{ "action": "load", "name": "MainScene" }
```

## 响应

成功: `{ "success": true, "data": ... }`. 失败: `{ "success": false, "message": "...", "error": "..." }`.
