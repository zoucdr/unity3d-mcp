---
name: unity-ugui-layout
description: UGUI layout tools.
---

# Unity MCP: ugui_layout

Pass parameters in `args`.

## Parameters (args)

| Param          | Type     | Required | Description |
|----------------|----------|----------|-------------|
| `action`       | string   | yes      | Operation: `set_anchor`, `set_pivot`, `set_size`, `apply_layout`, `set_anchored_position` |
| `target`       | string   | yes      | UI element path |
| `anchor_min`   | array    | no       | Anchor min [x, y] |
| `anchor_max`   | array    | no       | Anchor max [x, y] |
| `pivot`        | array    | no       | Pivot [x, y] |
| `anchored_position` | array | no   | Anchored position [x, y] |
| `size_delta`   | array    | no       | Size delta [width, height] |
| `layout_group` | string   | no       | Layout group type: `horizontal`, `vertical`, `grid` |
| `spacing`      | number   | no       | Spacing between elements |
| `child_force_expand` | boolean | no   | Child force expand |

- **set_anchor** — Set RectTransform anchor
- **set_pivot** — Set RectTransform pivot
- **set_size** — Set element size
- **apply_layout** — Apply layout group
- **set_anchored_position** — Set anchored position

## Example args

```json
{ "action": "set_anchor", "target": "Canvas/Panel", "anchor_min": [0, 0], "anchor_max": [1, 1] }
```

## Response

Success: `{ "success": true, "data": ... }`. Failure: `{ "success": false, "message": "...", "error": "..." }`.
