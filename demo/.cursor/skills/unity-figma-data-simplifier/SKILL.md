---
name: unity-figma-data-simplifier
description: Simplify Figma data for Unity UI conversion.
---

# Unity MCP: figma_data_simplifier

Pass parameters in `args`.

## Parameters (args)

| Param          | Type     | Required | Description |
|----------------|----------|----------|-------------|
| `action`       | string   | yes      | Operation: `simplify`, `export`, `preview` |
| `figma_file`   | string   | yes      | Figma file key |
| `node_ids`    | array    | no       | Array of node IDs to simplify |
| `output_path` | string   | no       | Output JSON path |
| `options`     | object   | no       | Simplification options |

Options:
- `remove_hidden`: boolean — Remove hidden nodes
- `flatten_groups`: boolean — Flatten group structure
- `optimize_frames`: boolean — Optimize frame data
- `merge_text`: boolean — Merge text styles

- **simplify** — Simplify Figma data structure
- **export** — Export simplified data
- **preview** — Preview simplified result

## Example args

```json
{ "action": "simplify", "figma_file": "abc123", "node_ids": ["1:1", "1:2"], "options": { "flatten_groups": true } }
```

## Response

Success: `{ "success": true, "data": ... }`. Failure: `{ "success": false, "message": "...", "error": "..." }`.
