---
name: unity-edit-texture
description: 编辑纹理属性。
---

# Unity MCP: edit_texture

通过 `args` 传递参数。

## 参数 (args)

| 参数           | 类型     | 必填 | 说明 |
|---------------|----------|------|------|
| `action`     | string   | 是   | 操作: `set_import_settings`, `get_info`, `set_wrap_mode`, `set_filter_mode` |
| `target`     | string   | 是   | 纹理资源路径 |
| `max_size`  | integer  | 否   | 最大纹理尺寸 |
| `compression`| string   | 否   | 压缩质量: `low`, `normal`, `high` |
| `texture_type`| string | 否   | 纹理类型: `texture`, `normal_map`, `sprite`, `cursor` |
| `wrap_mode` | string   | 否   | 包裹模式: `repeat`, `clamp`, `mirror` |
| `filter_mode`| string   | 否   | 过滤模式: `point`, `bilinear`, `trilinear` |
| `anisotropic`| integer  | 否   | 各向异性等级 |

- **set_import_settings** — 设置纹理导入设置
- **get_info** — 获取纹理信息
- **set_wrap_mode** — 设置纹理包裹模式
- **set_filter_mode** — 设置纹理过滤模式

## 示例参数

```json
{ "action": "set_import_settings", "target": "Assets/Textures/Player.png", "max_size": 2048, "compression": "high" }
```

## 响应

成功: `{ "success": true, "data": ... }`. 失败: `{ "success": false, "message": "...", "error": "..." }`.
