---
name: unity-console-write
description: 向 Unity 编辑器控制台写入日志消息。
---

# Unity MCP: console_write

通过 `args` 传递参数。

## 参数 (args)

| 参数         | 类型    | 必填 | 说明 |
|-------------|---------|------|------|
| `action`    | string  | 是   | 消息类型: `log`, `warning`, `error`, `assert`, `exception` |
| `message`   | string  | 是   | 日志消息内容 |
| `condition` | string  | 否   | 断言条件表达式 (仅用于 assert 类型) |
| `context`   | string  | 否   | 上下文对象名称 (用于在控制台中定位) |
| `tag`       | string  | 否   | 用于分类和过滤的标签 |

- **log** — 信息消息
- **warning** — 警告消息
- **error** — 错误消息
- **assert** — 断言消息
- **exception** — 异常消息

## 示例参数

```json
{ "action": "log", "message": "游戏已初始化", "tag": "启动" }
```

```json
{ "action": "error", "message": "资源加载失败", "context": "ResourceManager" }
```

## 响应

成功: `{ "success": true, "data": ... }`. 失败: `{ "success": false, "message": "...", "error": "..." }`.
