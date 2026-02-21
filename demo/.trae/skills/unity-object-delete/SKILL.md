---
name: unity-object-delete
description: 删除游戏对象或资源。
---

# Unity MCP: object_delete

通过 `args` 传递参数。

## 参数 (args)

| 参数         | 类型    | 必填 | 说明 |
|-------------|---------|------|------|
| `path`      | string  | 是   | 对象层级路径 |
| `instance_id`| string | 否   | 对象实例ID |
| `confirm`   | boolean | 否   | 强制确认: true=始终确认, false=智能确认 (≤3自动, >3对话框) |

## 示例参数

```json
{ "path": "Player/Cube" }
```

```json
{ "path": "Assets/Prefabs/Enemy.prefab", "confirm": true }
```

## 响应

成功: `{ "success": true, "data": ... }`. 失败: `{ "success": false, "message": "...", "error": "..." }`.
