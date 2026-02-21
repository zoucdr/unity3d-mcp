---
name: unity-source-location
description: Locate resources in file explorer or Unity project.
---

# Unity MCP: source_location

Pass parameters in `args`.

## Parameters (args)

| Param          | Type     | Required | Description |
|----------------|----------|----------|-------------|
| `action`       | string   | yes      | Operation: `reveal`, `locate`, `find_references` |
| `target`       | string   | yes      | Asset or GameObject path |
| `open_explorer`| boolean  | no       | Open in file explorer (default: true) |
| `focus_project`| boolean  | no       | Focus Unity project window |

- **reveal** — Reveal asset in file explorer
- **locate** — Locate in Unity project view
- **find_references** — Find all references to asset

## Example args

```json
{ "action": "reveal", "target": "Assets/Textures/Player.png" }
```

## Response

Success: `{ "success": true, "data": ... }`. Failure: `{ "success": false, "message": "...", "error": "..." }`.
