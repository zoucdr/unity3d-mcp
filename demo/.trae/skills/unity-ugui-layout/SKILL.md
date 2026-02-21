---
name: unity-ugui-layout
description: UGUI 布局工具。
---

# Unity MCP: ugui_layout

通过 `args` 传递参数。

## 参数 (args)

| 参数                 | 类型     | 必填 | 说明 |
|---------------------|----------|------|------|
| `action`           | string   | 是   | 操作: `set_anchor`, `set_pivot`, `set_size`, `apply_layout`, `set_anchored_position` |
| `target`           | string   | 是   | UI 元素路径 |
| `anchor_min`       | array    | 否   | 锚点最小值 [x, y] |
| `anchor_max`       | array    | 否   | 锚点最大值 [x, y] |
| `pivot`            | array    | 否   | 支点 [x, y] |
| `anchored_position`| array    | 否   | 锚定位置 [x, y] |
| `size_delta`       | array    | 否   | 尺寸增量 [宽, 高] |
| `layout_group`     | string   | 否   | 布局组类型: `horizontal`, `vertical`, `grid` |
| `spacing`          | number   | 否   | 元素间距 |
| `child_force_expand`| boolean  | 否   | 子项强制扩展 |

- **set_anchor** — 设置 RectTransform 锚点
- **set_pivot** — 设置 RectTransform 支点
- **set_size** — 设置元素尺寸
- **apply_layout** — 应用布局组
- **set_anchored_position** — 设置锚定位置

## 示例参数

```json
{ "action": "set_anchor", "target": "Canvas/Panel", "anchor_min": [0, 0], "anchor_max": [1, 1] }
```

## 响应

成功: `{ "success": true, "data": ... }`. 失败: `{ "success": false, "message": "...", "error": "..." }`.
