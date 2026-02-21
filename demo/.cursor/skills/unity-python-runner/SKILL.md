---
name: unity-python-runner
description: Execute Python scripts in Unity environment.
---

# Unity MCP: python_runner

Pass parameters in `args`.

## Parameters (args)

| Param          | Type     | Required | Description |
|----------------|----------|----------|-------------|
| `action`       | string   | yes      | Operation: `run`, `execute_file`, `eval` |
| `script`       | string   | yes      | Python script to execute |
| `file_path`   | string   | no       | Python file path to execute |
| `args`        | object   | no       | Arguments to pass to script |
| `timeout`     | integer  | no       | Timeout in seconds (default: 60) |
| `working_dir` | string   | no       | Working directory |

- **run** — Run Python script inline
- **execute_file** — Execute Python file
- **eval** — Quick Python expression evaluation

## Example args

```json
{ "action": "run", "script": "print('Hello from Python!')" }
```

## Response

Success: `{ "success": true, "data": ... }`. Failure: `{ "success": false, "message": "...", "error": "..." }`.
