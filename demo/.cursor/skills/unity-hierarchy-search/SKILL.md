---
name: unity-hierarchy-search
description: Search and find game objects in scene hierarchy.
---

# Unity MCP: hierarchy_search

Pass parameters in `args`.

## Parameters (args)

| Param             | Type     | Required | Description |
|-------------------|----------|----------|-------------|
| `search_type`     | string   | yes      | Search method: `by_name`, `by_id`, `by_tag`, `by_layer`, `by_component`, `by_query` |
| `query`           | string   | yes      | Search condition (ID, name, or path, supports wildcard *) |
| `include_inactive`| boolean  | no       | Include inactive objects (default: false) |
| `include_hierarchy`| boolean | no       | Include full hierarchy data (default: false) |
| `select_many`     | boolean  | no       | Find all matches (default: false) |
| `use_regex`       | boolean  | no       | Use regular expressions (default: false) |

- **by_name** — Search by object name
- **by_id** — Search by instance ID
- **by_tag** — Search by tag
- **by_layer** — Search by layer
- **by_component** — Search by component
- **by_query** — Query-based search

## Example args

```json
{ "search_type": "by_name", "query": "Player*" }
```

## Response

Success: `{ "success": true, "data": ... }`. Failure: `{ "success": false, "message": "...", "error": "..." }`.
