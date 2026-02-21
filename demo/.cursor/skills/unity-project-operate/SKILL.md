---
name: unity-project-operate
description: Manage Unity assets.
---

# Unity MCP: project_operate

Pass parameters in `args`.

## Parameters (args)

| Param            | Type     | Required | Description |
|------------------|----------|----------|-------------|
| `action`         | string   | yes      | Operation: `import`, `modify`, `move`, `duplicate`, `rename`, `get_info`, `create_folder`, `reload`, `select`, `ping`, `select_depends`, `select_usage`, `tree` |
| `path`           | string   | yes      | Asset path (Unity standard format) |
| `destination`   | string   | no       | Destination path (for move/duplicate) |
| `force`          | boolean  | no       | Force operation (overwrite existing) |
| `properties`     | object   | no       | Asset properties dictionary |
| `refresh_type`   | string   | no       | Refresh type: `all`, `assets`, `scripts` (default: all) |
| `save_before_refresh` | boolean | no   | Save all before refresh (default: true) |
| `include_indirect` | boolean | no       | Include indirect dependencies (default: false) |
| `max_results`   | integer  | no       | Maximum results (default: 100) |

- **import** — Import asset
- **modify** — Modify asset properties
- **move** — Move asset
- **duplicate** — Duplicate asset
- **rename** — Rename asset
- **get_info** — Get asset info
- **create_folder** — Create folder
- **reload** — Reload asset
- **select** — Select asset
- **ping** — Ping (highlight) asset
- **select_depends** — Select dependencies
- **select_usage** — Select usage
- **tree** — Show asset tree

## Example args

```json
{ "action": "move", "path": "Assets/OldFolder/file.cs", "destination": "Assets/NewFolder/file.cs" }
```

## Response

Success: `{ "success": true, "data": ... }`. Failure: `{ "success": false, "message": "...", "error": "..." }`.
