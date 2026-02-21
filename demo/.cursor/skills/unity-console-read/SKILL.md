---
name: unity-console-read
description: Read or clear Unity Editor console logs.
---

# Unity MCP: console_read

Invoke with `func: "console_read"`. Pass parameters in `args`.

## Parameters (args)

| Param       | Type    | Required | Description |
|------------|---------|----------|-------------|
| `action`   | string  | yes      | `get` (no stack), `get_full` (with stack), `clear` |
| `types`    | string  | no       | Comma-separated: `error`, `warning`, `log`; omit or `"all"` for all |
| `count`    | integer | no       | Max entries 1–1000 |
| `filterText` | string | no     | Only entries containing this text |
| `format`   | string  | no       | `plain`, `detailed` (default), `json` |

- **get** — logs without stack; use with count/types/filterText/format.
- **get_full** — logs with stack; same optional params.
- **clear** — clear console; no other params.

## Example args

```json
{ "action": "get", "count": 10 }
```

## Response

Success: `{ "success": true, "data": [ ... ] }`. Failure: `{ "success": false, "message": "...", "error": "..." }`.
