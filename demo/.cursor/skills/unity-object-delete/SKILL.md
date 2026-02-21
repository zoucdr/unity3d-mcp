---
name: unity-object-delete
description: Delete game objects or assets.
---

# Unity MCP: object_delete

Pass parameters in `args`.

## Parameters (args)

| Param        | Type    | Required | Description |
|--------------|---------|----------|-------------|
| `path`       | string  | yes      | Object hierarchy path |
| `instance_id`| string  | no       | Object instance ID |
| `confirm`    | boolean | no       | Force confirmation: true=always confirm, false=smart confirm (≤3 auto, >3 dialog) |

## Example args

```json
{ "path": "Player/Cube" }
```

```json
 { "path": "Assets/Prefabs/Enemy.prefab", "confirm": true }
```

## Response

Success: `{ "success": true, "data": ... }`. Failure: `{ "success": false, "message": "...", "error": "..." }`.
