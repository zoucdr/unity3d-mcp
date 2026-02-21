---
name: unity-edit-material
description: Edit material properties.
---

# Unity MCP: edit_material

Pass parameters in `args`.

## Parameters (args)

| Param         | Type     | Required | Description |
|---------------|----------|----------|-------------|
| `action`      | string   | yes      | Operation: `set_property`, `get_property`, `set_shader`, `set_color`, `set_texture` |
| `target`      | string   | yes      | Material path or GameObject with material |
| `property`   | string   | no       | Property name |
| `value`       | any      | no       | Property value |
| `shader_name` | string   | no       | Shader name (for set_shader) |
| `color`       | array    | no       | Color RGBA [r, g, b, a] |
| `texture_path`| string   | no       | Texture asset path |

- **set_property** — Set material property
- **get_property** — Get material property
- **set_shader** — Change material shader
- **set_color** — Set material color
- **set_texture** — Set material texture

## Example args

```json
{ "action": "set_color", "target": "Assets/Materials/Player.mat", "color": [1, 0, 0, 1] }
```

## Response

Success: `{ "success": true, "data": ... }`. Failure: `{ "success": false, "message": "...", "error": "..." }`.
