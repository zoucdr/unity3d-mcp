---
name: unity-edit-texture
description: Edit texture properties.
---

# Unity MCP: edit_texture

Pass parameters in `args`.

## Parameters (args)

| Param          | Type     | Required | Description |
|----------------|----------|----------|-------------|
| `action`       | string   | yes      | Operation: `set_import_settings`, `get_info`, `set_wrap_mode`, `set_filter_mode` |
| `target`       | string   | yes      | Texture asset path |
| `max_size`    | integer  | no       | Maximum texture size |
| `compression` | string   | no       | Compression quality: `low`, `normal`, `high` |
| `texture_type`| string   | no       | Texture type: `texture`, `normal_map`, `sprite`, `cursor` |
| `wrap_mode`   | string   | no       | Wrap mode: `repeat`, `clamp`, `mirror` |
| `filter_mode` | string   | no       | Filter mode: `point`, `bilinear`, `trilinear` |
| `anisotropic` | integer  | no       | Anisotropic level |

- **set_import_settings** — Set texture import settings
- **get_info** — Get texture information
- **set_wrap_mode** — Set texture wrap mode
- **set_filter_mode** — Set texture filter mode

## Example args

```json
{ "action": "set_import_settings", "target": "Assets/Textures/Player.png", "max_size": 2048, "compression": "high" }
```

## Response

Success: `{ "success": true, "data": ... }`. Failure: `{ "success": false, "message": "...", "error": "..." }`.
