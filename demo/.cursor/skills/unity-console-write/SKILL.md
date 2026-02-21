---
name: unity-console-write
description: Write log messages to Unity editor console.
---

# Unity MCP: console_write

Pass parameters in `args`.

## Parameters (args)

| Param        | Type    | Required | Description |
|--------------|---------|----------|-------------|
| `action`     | string  | yes      | Message type: `log`, `warning`, `error`, `assert`, `exception` |
| `message`    | string  | yes      | Log message content |
| `condition`  | string  | no       | Assert condition expression (only for assert type) |
| `context`    | string  | no       | Context object name for locating in console |
| `tag`        | string  | no       | Tag for categorization and filtering |

- **log** — Info message
- **warning** — Warning message
- **error** — Error message
- **assert** — Assertion message
- **exception** — Exception message

## Example args

```json
{ "action": "log", "message": "Game initialized", "tag": "Startup" }
```

```json
{ "action": "error", "message": "Failed to load resource", "context": "ResourceManager" }
```

## Response

Success: `{ "success": true, "data": ... }`. Failure: `{ "success": false, "message": "...", "error": "..." }`.
