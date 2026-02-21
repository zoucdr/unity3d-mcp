---
name: unity-project-create
description: 在项目窗口中创建资源。
---

# Unity MCP: project_create

通过 `args` 传递参数。

## 参数 (args)

| 参数                 | 类型     | 必填 | 说明 |
|--------------------|----------|------|------|
| `name`            | string   | 是   | 资源文件名 |
| `source`          | string   | 是   | 操作类型: `menu`, `empty`, `template`, `copy` |
| `folder_path`    | string   | 否   | 目标文件夹路径 (相对于 Assets) |
| `extension`      | string   | 否   | 文件扩展名 (不带点): `cs`, `mat`, `prefab`, `asset`, `txt`, `json` |
| `content`        | string   | 否   | 文件内容 (用于 empty 类型) |
| `copy_source`    | string   | 否   | 要复制的源资源路径 |
| `template_path`  | string   | 否   | 模板文件路径 |
| `menu_path`      | string   | 否   | 菜单路径 |
| `open_after_create` | boolean | 否   | 创建后打开文件 |
| `select_after_create`| boolean | 否   | 创建后选中文件 |
| `force`          | boolean  | 否   | 强制覆盖现有文件 |

- **menu** — 从 Unity 菜单创建
- **empty** — 创建空文件
- **template** — 从模板创建
- **copy** — 从现有资源复制

## 示例参数

```json
{ "name": "PlayerController", "source": "empty", "extension": "cs", "folder_path": "Scripts" }
```

## 响应

成功: `{ "success": true, "data": ... }`. 失败: `{ "success": false, "message": "...", "error": "..." }`.
