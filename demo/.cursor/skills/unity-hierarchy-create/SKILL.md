---
name: unity-hierarchy-create
description: Create game objects in scene hierarchy.
---

# Unity MCP: hierarchy_create

Pass parameters in `args`.

## Parameters (args)

| Param          | Type     | Required | Description |
|----------------|----------|----------|-------------|
| `name`         | string   | yes      | Game object name |
| `source`       | string   | yes      | Source type: `primitive`, `menu`, `prefab`, `empty`, `copy` |
| `parent`       | string   | no       | Parent object name or path |
| `parent_id`    | string   | no       | Parent object unique ID |
| `position`     | array    | no       | Position [x, y, z] |
| `rotation`     | array    | no       | Rotation [x, y, z] |
| `scale`        | array    | no       | Scale [x, y, z] |
| `layer`        | string   | no       | Game object layer |
| `tag`          | string   | no       | Game object tag |
| `set_active`   | boolean  | no       | Set active state |
| `prefab_path`  | string   | no       | Prefab path (for prefab source) |
| `copy_source`  | string   | no       | Source object to copy (for copy source) |
| `menu_path`    | string   | no       | Menu path (for menu source) |
| `primitive_type` | string | no       | Primitive type: `Cube`, `Sphere`, `Capsule`, `Cylinder`, `Plane`, `Quad` |
| `save_as_prefab` | boolean | no     | Save as prefab |

- **primitive** — Create primitive object (Cube, Sphere, etc.)
- **prefab** — Instantiate from prefab
- **copy** — Copy existing object
- **menu** — Create from Unity menu
- **empty** — Create empty game object

## Example args

```json
{ "name": "Player", "source": "primitive", "primitive_type": "Cube", "position": [0, 1, 0] }
```

## Response

Success: `{ "success": true, "data": ... }`. Failure: `{ "success": false, "message": "...", "error": "..." }`.
