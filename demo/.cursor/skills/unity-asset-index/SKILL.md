---
name: unity-asset-index
description: Resource index management for Unity projects.
---

# Unity MCP: asset_index

Invoke with `func: "asset_index"`. Pass parameters in `args`.

## Parameters (args)

| Param     | Type    | Required | Description |
|-----------|---------|----------|-------------|
| `action`  | string  | yes      | Operation type: `add`, `remove`, `update`, `search`, `categories`, `locate`, `details`, `show`, `lost` |
| `id`      | integer | no       | Unique resource ID (for remove, update, locate operations) |
| `name`    | string  | no       | Resource name |
| `path`    | string  | no       | Resource path |
| `category`| string  | no       | Category label for organizing resources (default: "默认") |
| `note`    | string  | no       | Note information |
| `pattern` | string  | no       | Regex pattern (for search operation, matches name, note, path) |

- **add** — Add a new resource to index
- **remove** — Remove resource from index
- **update** — Update existing resource info
- **search** — Search resources by pattern
- **categories** — List all categories
- **locate** — Locate resource by ID
- **details** — Get detailed resource info
- **show** — Show all resources
- **lost** — Find resources with broken references

## Example args

Add a resource:
```json
{ "action": "add", "name": "PlayerPrefab", "path": "Assets/Prefabs/Player.prefab", "category": "Prefabs" }
```

Search resources:
```json
{ "action": "search", "pattern": "Player*" }
```

## Response

Success: `{ "success": true, "data": ... }`. Failure: `{ "success": false, "message": "...", "error": "..." }`.
