---
name: unity-asset-index
description: 资源索引管理。
---

# Unity MCP: asset_index

通过 `args` 传递参数。

## 参数 (args)

| 参数       | 类型     | 必填 | 说明 |
|------------|----------|------|------|
| `action`   | string   | 是   | 操作类型: `add`, `remove`, `update`, `search`, `categories`, `locate`, `details`, `show`, `lost` |
| `id`       | integer  | 否   | 资源唯一ID (用于 remove, update, locate) |
| `name`     | string   | 否   | 资源名称 |
| `path`     | string   | 否   | 资源路径 |
| `category` | string   | 否   | 分类标签 (默认: "默认") |
| `note`     | string   | 否   | 备注信息 |
| `pattern`  | string   | 否   | 正则表达式 (用于搜索，匹配名称、备注、路径) |

- **add** — 添加资源到索引
- **remove** — 从索引移除资源
- **update** — 更新资源信息
- **search** — 按模式搜索资源
- **categories** — 列出所有分类
- **locate** — 按ID定位资源
- **details** — 获取资源详细信息
- **show** — 显示所有资源
- **lost** — 查找引用断裂的资源

## 示例参数

添加资源:
```json
{ "action": "add", "name": "PlayerPrefab", "path": "Assets/Prefabs/Player.prefab", "category": "Prefabs" }
```

搜索资源:
```json
{ "action": "search", "pattern": "Player*" }
```

## 响应

成功: `{ "success": true, "data": ... }`. 失败: `{ "success": false, "message": "...", "error": "..." }`.
