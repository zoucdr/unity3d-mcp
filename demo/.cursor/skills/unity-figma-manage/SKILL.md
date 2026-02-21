---
name: unity-figma-manage
description: Manage Figma documents.
---

# Unity MCP: figma_manage

Pass parameters in `args`.

## Parameters (args)

| Param              | Type     | Required | Description |
|--------------------|----------|----------|-------------|
| `action`           | string   | yes      | Operation: `fetch_document`, `download_images`, `preview`, `get_conversion_rules` |
| `file_key`         | string   | no       | Figma file key |
| `node_id`          | string   | no       | Node ID |
| `local_json_path`  | string   | no       | Local JSON file path |
| `save_path`        | string   | no       | Save path |
| `image_format`     | string   | no       | Image format: `png`, `jpg`, `svg`, `pdf` |
| `image_scale`      | number   | no       | Image scale factor |
| `include_children` | boolean  | no       | Include child nodes |
| `ui_framework`     | string   | no       | UI framework: `ugui`, `uitoolkit`, `all` |
| `use_component_pfb` | boolean | no       | Use existing prefabs |
| `node_imgs`        | object   | no       | Node image mapping |

- **fetch_document** — Fetch Figma document structure
- **download_images** — Download images from Figma
- **preview** — Preview Figma designs
- **get_conversion_rules** — Get conversion rules for UI

## Example args

```json
{ "action": "fetch_document", "file_key": "abc123" }
```

## Response

Success: `{ "success": true, "data": ... }`. Failure: `{ "success": false, "message": "...", "error": "..." }`.
