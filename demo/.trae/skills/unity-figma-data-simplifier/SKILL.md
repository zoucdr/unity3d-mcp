---
name: unity-figma-data-simplifier
description: 简化 Figma 数据以转换为 Unity UI。
---

# Unity MCP: figma_data_simplifier

通过 `args` 传递参数。

## 参数 (args)

| 参数           | 类型     | 必填 | 说明 |
|---------------|----------|------|------|
| `action`     | string   | 是   | 操作: `simplify`, `export`, `preview` |
| `figma_file` | string   | 是   | Figma 文件密钥 |
| `node_ids`  | array    | 否   | 要简化的节点 ID 数组 |
| `output_path`| string   | 否   | 输出 JSON 路径 |
| `options`   | object   | 否   | 简化选项 |

选项:
- `remove_hidden`: boolean — 移除隐藏节点
- `flatten_groups`: boolean — 扁平化组结构
- `optimize_frames`: boolean — 优化帧数据
- `merge_text`: boolean — 合并文本样式

- **simplify** — 简化 Figma 数据结构
- **export** — 导出简化数据
- **preview** — 预览简化结果

## 示例参数

```json
{ "action": "simplify", "figma_file": "abc123", "node_ids": ["1:1", "1:2"], "options": { "flatten_groups": true } }
```

## 响应

成功: `{ "success": true, "data": ... }`. 失败: `{ "success": false, "message": "...", "error": "..." }`.
