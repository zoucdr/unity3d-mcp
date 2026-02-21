---
name: unity-project-search
description: Search assets in project window.
---

# Unity MCP: project_search

Pass parameters in `args`.

## Parameters (args)

| Param             | Type     | Required | Description |
|-------------------|----------|----------|-------------|
| `query`           | string   | yes      | Search keywords |
| `search_target`  | string   | no       | Search type: `asset`, `folder`, `script`, `texture`, `material`, `prefab`, `scene`, `audio`, `model` |
| `directory`       | string   | no       | Search path (relative to Assets) |
| `file_extension`  | string   | no       | File extension filter: `cs`, `mat`, `prefab`, `unity`, `png`, `jpg`, `fbx`, `wav`, `mp3` |
| `case_sensitive`  | boolean  | no       | Case sensitive search (default: false) |
| `recursive`       | boolean  | no       | Recursive search subfolders (default: true) |
| `include_meta`    | boolean  | no       | Include .meta files (default: false) |
| `max_results`    | integer  | no       | Maximum results (default: 100) |

## Example args

```json
{ "query": "Player", "search_target": "prefab" }
```

## Response

Success: `{ "success": true, "data": ... }`. Failure: `{ "success": false, "message": "...", "error": "..." }`.
