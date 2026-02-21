---
name: unity-edit-scene
description: Manage scene assets.
---

# Unity MCP: edit_scene

Pass parameters in `args`.

## Parameters (args)

| Param        | Type    | Required | Description |
|--------------|---------|----------|-------------|
| `action`     | string  | yes      | Operation: `load`, `save`, `create`, `get_hierarchy` |
| `name`       | string  | no       | Scene name |
| `path`       | string  | no       | Scene asset path |
| `build_index`| integer | no       | Build index |

- **load** — Load a scene
- **save** — Save current scene
- **create** — Create a new scene
- **get_hierarchy** — Get scene hierarchy

## Example args

Load scene:
```json
{ "action": "load", "name": "MainScene" }
```

## Response

Success: `{ "success": true, "data": ... }`. Failure: `{ "success": false, "message": "...", "error": "..." }`.
