---
name: unity-edit-component
description: Edit component properties on GameObjects.
---

# Unity MCP: edit_component

Pass parameters in `args`.

## Parameters (args)

| Param        | Type     | Required | Description |
|--------------|----------|----------|-------------|
| `action`     | string   | yes      | Operation: `set_property`, `get_property`, `list_components` |
| `target`     | string   | yes      | Target GameObject path |
| `component` | string   | yes      | Component type name |
| `property`  | string   | no       | Property name |
| `value`     | any      | no       | Property value to set |

- **set_property** — Set component property
- **get_property** — Get component property
- **list_components** — List all components on GameObject

## Example args

```json
{ "action": "set_property", "target": "Player", "component": "Rigidbody", "property": "mass", "value": 1.5 }
```

## Response

Success: `{ "success": true, "data": ... }`. Failure: `{ "success": false, "message": "...", "error": "..." }`.
