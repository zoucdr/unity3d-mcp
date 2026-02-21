---
name: unity-batch-call
description: Batch execution of multiple function calls in sequence.
---

# Unity MCP: batch_call

Pass parameters in `args`.

## Parameters (args)

| Param   | Type    | Required | Description |
|---------|---------|----------|-------------|
| `args`  | array   | yes      | List of function calls to execute sequentially |

Each item in the array should contain:
| Param   | Type    | Required | Description |
|---------|---------|----------|-------------|
| `func`  | string  | yes      | Function name to call |
| `args`  | object  | yes      | Arguments for the function |

## Example args

```json
{
  "args": [
    { "func": "hierarchy_create", "args": { "name": "Object1", "source": "primitive", "primitive_type": "Cube" } },
    { "func": "hierarchy_create", "args": { "name": "Object2", "source": "primitive", "primitive_type": "Sphere" } }
  ]
}
```

## Response

Success: `{ "success": true, "data": [ ... ] }`. Failure: `{ "success": false, "message": "...", "error": "..." }`.

Each function in the batch will be executed in order. Results are returned as an array.
