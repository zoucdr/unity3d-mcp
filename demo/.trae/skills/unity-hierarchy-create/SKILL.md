---
name: unity-hierarchy-create
description: 在场景层级中创建游戏对象。
---

# Unity MCP: hierarchy_create

通过 `args` 传递参数。

## 参数 (args)

| 参数              | 类型     | 必填 | 说明 |
|------------------|----------|------|------|
| `name`           | string   | 是   | 游戏对象名称 |
| `source`         | string   | 是   | 源类型: `primitive`, `menu`, `prefab`, `empty`, `copy` |
| `parent`         | string   | 否   | 父对象名称或路径 |
| `parent_id`      | string   | 否   | 父对象唯一ID |
| `position`       | array    | 否   | 位置 [x, y, z] |
| `rotation`       | array    | 否   | 旋转 [x, y, z] |
| `scale`          | array    | 否   | 缩放 [x, y, z] |
| `layer`          | string   | 否   | 游戏对象层 |
| `tag`            | string   | 否   | 游戏对象标签 |
| `set_active`     | boolean  | 否   | 设置激活状态 |
| `prefab_path`    | string   | 否   | 预制件路径 (用于 prefab 源) |
| `copy_source`    | string   | 否   | 要复制的源对象 (用于 copy 源) |
| `menu_path`      | string   | 否   | 菜单路径 (用于 menu 源) |
| `primitive_type` | string   | 否   | 基元类型: `Cube`, `Sphere`, `Capsule`, `Cylinder`, `Plane`, `Quad` |
| `save_as_prefab` | boolean  | 否   | 保存为预制件 |

- **primitive** — 创建基元对象 (Cube, Sphere 等)
- **prefab** — 从预制件实例化
- **copy** — 复制现有对象
- **menu** — 从 Unity 菜单创建
- **empty** — 创建空游戏对象

## 示例参数

```json
{ "name": "Player", "source": "primitive", "primitive_type": "Cube", "position": [0, 1, 0] }
```

## 响应

成功: `{ "success": true, "data": ... }`. 失败: `{ "success": false, "message": "...", "error": "..." }`.
