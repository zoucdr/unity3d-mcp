---
name: unity-edit-anim-clip
description: Manage animation clips.
---

# Unity MCP: edit_anim_clip

Pass parameters in `args`.

## Parameters (args)

| Param                      | Type     | Required | Description |
|----------------------------|----------|----------|-------------|
| `action`                   | string   | yes      | Operation: `create`, `modify`, `duplicate`, `delete`, `get_info`, `search`, `set_curve`, `set_events` |
| `path`                     | string   | no       | Animation clip asset path |
| `query`                    | string   | no       | Search query (for search action) |
| `recursive`                | boolean  | no       | Recursive search (default: false) |
| `source_path`              | string   | no       | Source animation path (for duplicate) |
| `destination`              | string   | no       | Destination path |
| `force`                    | boolean  | no       | Force execution (default: false) |
| `length`                   | number   | no       | Animation length (seconds) |
| `loop_time`                | boolean  | no       | Loop animation |
| `loop_pose`                | boolean  | no       | Loop pose |
| `frame_rate`               | number   | no       | Frame rate |
| `mirror`                   | boolean  | no       | Mirror animation |
| `cycle_offset`             | number   | no       | Cycle offset |
| `body_orientation`         | number   | no       | Body orientation |
| `height_from_ground`       | boolean  | no       | Height from ground |
| `lock_root_height_y`       | boolean  | no       | Lock root height Y |
| `lock_root_rotation_y`     | boolean  | no       | Lock root rotation Y |
| `lock_root_rotation_offset_y` | boolean | no    | Lock root rotation offset Y |
| `root_height_offset_y`     | number   | no       | Root height offset Y |
| `root_height_offset_y_active` | boolean | no     | Enable root height offset |
| `root_rotation_offset_y`   | number   | no       | Root rotation offset Y |
| `keep_original_orientation_y` | boolean | no    | Keep original orientation Y |
| `curves`                   | object   | no       | Animation curve data |
| `events`                   | array    | no       | Animation events |

## Example args

Create animation:
```json
{ "action": "create", "path": "Assets/Animations/NewAnim.anim", "length": 2.0, "loop_time": true }
```

## Response

Success: `{ "success": true, "data": ... }`. Failure: `{ "success": false, "message": "...", "error": "..." }`.
