---
name: unity-prefers
description: 管理 EditorPrefs 和 PlayerPrefs。
---

# Unity MCP: prefers

通过 `args` 传递参数。

## 参数 (args)

| 参数           | 类型     | 必填 | 说明 |
|---------------|----------|------|------|
| `action`     | string   | 是   | 操作: `get`, `set`, `delete`, `list`, `clear` |
| `pref_type` | string   | 是   | 偏好类型: `editor` (EditorPrefs), `player` (PlayerPrefs) |
| `key`       | string   | 否   | 偏好键 |
| `value`     | any      | 否   | 偏好值 |
| `value_type`| string   | 否   | 值类型: `string`, `int`, `float` |

- **get** — 获取偏好值
- **set** — 设置偏好值
- **delete** — 删除偏好
- **list** — 列出所有偏好
- **clear** — 清除所有偏好

## 示例参数

```json
{ "action": "get", "pref_type": "editor", "key": "LastProjectPath" }
```

## 响应

成功: `{ "success": true, "data": ... }`. 失败: `{ "success": false, "message": "...", "error": "..." }`.
