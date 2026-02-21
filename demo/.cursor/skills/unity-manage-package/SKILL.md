---
name: unity-manage-package
description: Manage Unity packages.
---

# Unity MCP: manage_package

Pass parameters in `args`.

## Parameters (args)

| Param          | Type     | Required | Description |
|----------------|----------|----------|-------------|
| `action`       | string   | yes      | Operation: `install`, `remove`, `update`, `list`, `search` |
| `package_name` | string   | no       | Package name (e.g., com.unity.render-pipelines.universal) |
| `version`     | string   | no       | Package version |
| `registry`    | string   | no       | Package registry URL |
| `scope`       | string   | no       | Package scope |
| `cached`      | boolean  | no       | Use cached version |

- **install** — Install a package
- **remove** — Remove a package
- **update** — Update a package
- **list** — List installed packages
- **search** — Search packages in registry

## Example args

```json
{ "action": "install", "package_name": "com.unity.collab-proxy", "version": "2.0.0" }
```

## Response

Success: `{ "success": true, "data": ... }`. Failure: `{ "success": false, "message": "...", "error": "..." }`.
