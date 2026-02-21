---
name: unity-cmd-tool
description: Execute system CMD commands.
---

# Unity MCP: cmd_tool

Pass parameters in `args`.

## Parameters (args)

| Param         | Type     | Required | Description |
|---------------|----------|----------|-------------|
| `cmd`         | string   | yes      | CMD command to execute |
| `args`        | array    | no       | Command arguments |
| `env`         | object   | no       | Environment variables |
| `timeout`     | integer  | no       | Timeout in seconds (default: 30) |
| `working_dir` | string   | no       | Working directory |

## Example args

```json
{ "cmd": "ls", "args": ["-la"], "working_dir": "/Users/zht/WorkSpace/project" }
```

## Response

Success: `{ "success": true, "data": ... }`. Failure: `{ "success": false, "message": "...", "error": "..." }`.
