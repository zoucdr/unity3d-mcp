---
name: unity-ui-rule-manage
description: Manage UI creation schemes and modification records.
---

# Unity MCP: ui_rule_manage

Pass parameters in `args`.

## Parameters (args)

| Param              | Type     | Required | Description |
|--------------------|----------|----------|-------------|
| `action`           | string   | yes      | Operation: `record_modify`, `record_renames`, `get_renames`, `record_download_sprites`, `get_download_sprites` |
| `name`             | string   | yes      | UI name |
| `modify_desc`     | string   | no       | Modification description |
| `names_data`      | object   | no       | Node name data |
| `properties`      | object   | no       | Properties data |
| `sprites_data`    | object   | no       | Sprites data |
| `save_path`       | string   | no       | Save path |
| `auto_load_sprites` | boolean | no       | Auto load sprites |

- **record_modify** — Record UI modifications
- **record_renames** — Record node renames
- **get_renames** — Get rename records
- **record_download_sprites** — Record downloaded sprites
- **get_download_sprites** — Get downloaded sprites

## Example args

```json
{ "action": "record_modify", "name": "MainMenu", "modify_desc": "Updated button positions" }
```

## Response

Success: `{ "success": true, "data": ... }`. Failure: `{ "success": false, "message": "...", "error": "..." }`.
