---
name: unity-prefers
description: Manage EditorPrefs and PlayerPrefs.
---

# Unity MCP: prefers

Pass parameters in `args`.

## Parameters (args)

| Param          | Type     | Required | Description |
|----------------|----------|----------|-------------|
| `action`       | string   | yes      | Operation: `get`, `set`, `delete`, `list`, `clear` |
| `pref_type`   | string   | yes      | Preference type: `editor` (EditorPrefs), `player` (PlayerPrefs) |
| `key`         | string   | no       | Preference key |
| `value`       | any      | no       | Preference value |
| `value_type`  | string   | no       | Value type: `string`, `int`, `float` |

- **get** — Get preference value
- **set** — Set preference value
- **delete** — Delete preference
- **list** — List all preferences
- **clear** — Clear all preferences

## Example args

```json
{ "action": "get", "pref_type": "editor", "key": "LastProjectPath" }
```

## Response

Success: `{ "success": true, "data": ... }`. Failure: `{ "success": false, "message": "...", "error": "..." }`.
