---
name: unity-figma-manage
description: 管理 Figma 文档。
---

# Unity MCP: figma_manage

通过 `args` 传递参数。

## 参数 (args)

| 参数                 | 类型     | 必填 | 说明 |
|---------------------|----------|------|------|
| `action`            | string   | 是   | 操作: `fetch_document`, `download_images`, `preview`, `get_conversion_rules` |
| `file_key`          | string   | 否   | Figma 文件密钥 |
| `node_id`           | string   | 否   | 节点 ID |
| `local_json_path`   | string   | 否   | 本地 JSON 文件路径 |
| `save_path`         | string   | 否   | 保存路径 |
| `image_format`      | string   | 否   | 图片格式: `png`, `jpg`, `svg`, `pdf` |
| `image_scale`       | number   | 否   | 图片缩放比例 |
| `include_children`  | boolean  | 否   | 包含子节点 |
| `ui_framework`      | string   | 否   | UI 框架: `ugui`, `uitoolkit`, `all` |
| `use_component_pfb` | boolean  | 否   | 使用现有预制件 |
| `node_imgs`         | object   | 否   | 节点图片映射 |

- **fetch_document** — 获取 Figma 文档结构
- **download_images** — 从 Figma 下载图片
- **preview** — 预览 Figma 设计
- **get_conversion_rules** — 获取 UI 转换规则

## 示例参数

```json
{ "action": "fetch_document", "file_key": "abc123" }
```

## 响应

成功: `{ "success": true, "data": ... }`. 失败: `{ "success": false, "message": "...", "error": "..." }`.
