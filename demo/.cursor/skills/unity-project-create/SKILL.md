---
name: unity-project-create
description: Create assets in project window.
---

# Unity MCP: project_create

Pass parameters in `args`.

## Parameters (args)

| Param            | Type     | Required | Description |
|------------------|----------|----------|-------------|
| `name`           | string   | yes      | Asset file name |
| `source`         | string   | yes      | Operation type: `menu`, `empty`, `template`, `copy` |
| `folder_path`    | string   | no       | Target folder path (relative to Assets) |
| `extension`      | string   | no       | File extension (without dot): `cs`, `mat`, `prefab`, `asset`, `txt`, `json` |
| `content`        | string   | no       | File content (for empty type) |
| `copy_source`    | string   | no       | Source resource path to copy |
| `template_path`  | string   | no       | Template file path |
| `menu_path`      | string   | no       | Menu path |
| `open_after_create` | boolean | no   | Open file after creation |
| `select_after_create`| boolean | no   | Select file after creation |
| `force`          | boolean  | no       | Force overwrite existing file |

- **menu** — Create from Unity menu
- **empty** — Create empty file
- **template** — Create from template
- **copy** — Copy from existing resource

## Example args

```json
{ "name": "PlayerController", "source": "empty", "extension": "cs", "folder_path": "Scripts" }
```

## Response

Success: `{ "success": true, "data": ... }`. Failure: `{ "success": false, "message": "...", "error": "..." }`.
