---
name: unity-console-read
description: 读取或清除 Unity 编辑器控制台日志。
---

# Unity MCP: console_read

通过 `args` 传递参数。

## 参数 (args)

| 参数          | 类型    | 必填 | 说明 |
|--------------|---------|------|------|
| `action`     | string  | 是   | 操作类型: `get` (无堆栈), `get_full` (带堆栈), `clear` |
| `types`      | string  | 否   | 逗号分隔: `error`, `warning`, `log`; 空或 `"all"` 表示全部 |
| `count`      | integer | 否   | 最大条目数 1–1000 |
| `filterText` | string  | 否   | 仅包含此文本的条目 |
| `format`     | string  | 否   | `plain`, `detailed` (默认), `json` |

- **get** — 无堆栈日志
- **get_full** — 带堆栈日志
- **clear** — 清除控制台

## 示例参数

```json
{ "action": "get", "count": 10 }
```

## 响应

成功: `{ "success": true, "data": [ ... ] }`. 失败: `{ "success": false, "message": "...", "error": "..." }`.
