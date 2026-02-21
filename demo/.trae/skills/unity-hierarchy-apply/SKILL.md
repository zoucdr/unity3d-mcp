---
name: unity-hierarchy-apply
description: 处理预制件应用和连接操作。
---

# Unity MCP: hierarchy_apply

通过 `args` 传递参数。

## 参数 (args)

| 参数            | 类型    | 必填 | 说明 |
|----------------|---------|------|------|
| `action`       | string  | 是   | 操作: `apply` |
| `target_object`| string  | 是   | 目标游戏对象层级路径 |
| `apply_type`  | string  | 否   | 连接类型: `connect_to_prefab`, `apply_prefab_changes`, `break_prefab_connection` |
| `prefab_path` | string  | 否   | 预制件路径 |
| `force_apply` | boolean | 否   | 强制创建连接 (覆盖现有) |

- **apply** — 应用预制件更改或管理预制件连接

## 示例参数

```json
{ "action": "apply", "target_object": "Player", "apply_type": "apply_prefab_changes" }
```

## 响应

成功: `{ "success": true, "data": ... }`. 失败: `{ "success": false, "message": "...", "error": "..." }`.
