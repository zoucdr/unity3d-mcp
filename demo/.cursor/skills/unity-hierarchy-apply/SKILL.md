---
name: unity-hierarchy-apply
description: Handle prefab apply and connection operations.
---

# Unity MCP: hierarchy_apply

Pass parameters in `args`.

## Parameters (args)

| Param           | Type    | Required | Description |
|-----------------|---------|----------|-------------|
| `action`        | string  | yes      | Operation: `apply` |
| `target_object` | string  | yes      | Target game object hierarchy path |
| `apply_type`   | string  | no       | Connection type: `connect_to_prefab`, `apply_prefab_changes`, `break_prefab_connection` |
| `prefab_path`   | string  | no       | Prefab path |
| `force_apply`   | boolean | no       | Force create connection (override existing) |

- **apply** — Apply prefab changes or manage prefab connections

## Example args

```json
{ "action": "apply", "target_object": "Player", "apply_type": "apply_prefab_changes" }
```

## Response

Success: `{ "success": true, "data": ... }`. Failure: `{ "success": false, "message": "...", "error": "..." }`.
