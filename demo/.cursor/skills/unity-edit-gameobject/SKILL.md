---
name: unity-edit-gameobject
description: Edit GameObject properties.
---

# Unity MCP: edit_gameobject

Pass parameters in `args`.

## Parameters (args)

| Param        | Type     | Required | Description |
|--------------|----------|----------|-------------|
| `action`     | string   | yes      | Operation: `set_property`, `get_property`, `add_component`, `remove_component`, `set_transform` |
| `target`     | string   | yes      | Target GameObject path or name |
| `property`  | string   | no       | Property name to set/get |
| `value`      | any      | no       | Property value |
| `component` | string   | no       | Component type to add/remove |
| `position`   | array    | no       | Transform position [x, y, z] |
| `rotation`   | array    | no       | Transform rotation [x, y, z] |
| `scale`      | array    | no       | Transform scale [x, y, z] |

- **set_property** — Set a GameObject property
- **get_property** — Get a GameObject property
- **add_component** — Add a component to GameObject
- **remove_component** — Remove a component from GameObject
- **set_transform** — Set transform properties

## Example args

```json
{ "action": "set_property", "target": "Player", "property": "active", "value": true }
```

## Response

Success: `{ "success": true, "data": ... }`. Failure: `{ "success": false, "message": "...", "error": "..." }`.
