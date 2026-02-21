---
name: unity-edit-material
description: 编辑材质属性。
---

# Unity MCP: edit_material

通过 `args` 传递参数。

## 参数 (args)

| 参数           | 类型     | 必填 | 说明 |
|---------------|----------|------|------|
| `action`     | string   | 是   | 操作: `set_property`, `get_property`, `set_shader`, `set_color`, `set_texture` |
| `target`     | string   | 是   | 材质路径或带有材质的 GameObject |
| `property`   | string   | 否   | 属性名称 |
| `value`      | any      | 否   | 属性值 |
| `shader_name`| string   | 否   | 着色器名称 (用于 set_shader) |
| `color`      | array    | 否   | 颜色 RGBA [r, g, b, a] |
| `texture_path`| string   | 否   | 纹理资源路径 |

- **set_property** — 设置材质属性
- **get_property** — 获取材质属性
- **set_shader** — 更改材质着色器
- **set_color** — 设置材质颜色
- **set_texture** — 设置材质纹理

## 示例参数

```json
{ "action": "set_color", "target": "Assets/Materials/Player.mat", "color": [1, 0, 0, 1] }
```

## 响应

成功: `{ "success": true, "data": ... }`. 失败: `{ "success": false, "message": "...", "error": "..." }`.
