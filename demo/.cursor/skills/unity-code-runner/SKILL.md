---
name: unity-code-runner
description: Execute C# code at runtime in Unity.
---

# Unity MCP: code_runner

Pass parameters in `args`.

## Parameters (args)

| Param          | Type     | Required | Description |
|----------------|----------|----------|-------------|
| `action`       | string   | yes      | Operation: `run`, `compile`, `eval` |
| `code`         | string   | yes      | C# code to execute |
| `context`     | object   | no       | Execution context/variables |
| `timeout`     | integer  | no       | Timeout in seconds (default: 30) |
| `assembly_name`| string   | no       | Target assembly name |

- **run** — Compile and run C# code
- **compile** — Compile code without running
- **eval** — Quick evaluation expression

## Example args

```json
{ "action": "run", "code": "Debug.Log(\"Hello from Unity!\");" }
```

## Response

Success: `{ "success": true, "data": ... }`. Failure: `{ "success": false, "message": "...", "error": "..." }`.
