---
name: unity-async-call
description: Asynchronous function call support for Unity.
---

# Unity MCP: async_call

Invoke with `func: "async_call"`. Pass parameters in `args`.

## Parameters (args)

| Param   | Type    | Required | Description |
|---------|---------|----------|-------------|
| `id`    | string  | yes      | Unique identifier for the async call |
| `type`  | string  | yes      | Operation type: `in` (start call), `out` (get result) |
| `func`  | string  | no       | Function name (required when type is `in`) |
| `args`  | object  | no       | Function arguments (required when type is `in`) |

- **type: `in`** — Initiates an asynchronous function call. Requires `func` and `args`.
- **type: `out`** — Retrieves the result of a previous async call. Requires `id`.

## Example args

Start an async call:
```json
{ "id": "call_001", "type": "in", "func": "SomeFunction", "args": { "param1": "value1" } }
```

Get result:
```json
{ "id": "call_001", "type": "out" }
```

## Response

Success: `{ "success": true, "data": ... }`. Failure: `{ "success": false, "message": "...", "error": "..." }`.
