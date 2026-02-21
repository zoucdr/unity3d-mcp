---
name: unity-webpage-manage
description: Manage web pages.
---

# Unity MCP: webpage_manage

Pass parameters in `args`.

## Parameters (args)

| Param         | Type     | Required | Description |
|---------------|----------|----------|-------------|
| `action`      | string   | yes      | Operation: `add`, `remove`, `update`, `search`, `categories`, `open`, `details` |
| `url`         | string   | no       | Website URL |
| `description` | string   | no       | Website name or description |
| `category`    | string   | no       | Category label (default: "默认") |
| `id`          | integer  | no       | Web page unique ID (for remove/update) |
| `pattern`     | string   | no       | Regex pattern (for search, matches URL, description, category, note) |
| `note`        | string   | no       | Note information |

- **add** — Add a new web page
- **remove** — Remove web page
- **update** — Update web page info
- **search** — Search web pages by pattern
- **categories** — List all categories
- **open** — Open web page
- **details** — Get web page details

## Example args

```json
{ "action": "add", "url": "https://docs.unity.com", "description": "Unity Documentation", "category": "Docs" }
```

## Response

Success: `{ "success": true, "data": ... }`. Failure: `{ "success": false, "message": "...", "error": "..." }`.
