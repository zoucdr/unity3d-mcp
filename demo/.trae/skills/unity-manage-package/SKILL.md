---
name: unity-manage-package
description: 管理 Unity 包。
---

# Unity MCP: manage_package

通过 `args` 传递参数。

## 参数 (args)

| 参数           | 类型     | 必填 | 说明 |
|---------------|----------|------|------|
| `action`     | string   | 是   | 操作: `install`, `remove`, `update`, `list`, `search` |
| `package_name`| string   | 否   | 包名称 (如 com.unity.render-pipelines.universal) |
| `version`   | string   | 否   | 包版本 |
| `registry`  | string   | 否   | 包注册表 URL |
| `scope`     | string   | 否   | 包作用域 |
| `cached`    | boolean  | 否   | 使用缓存版本 |

- **install** — 安装包
- **remove** — 移除包
- **update** — 更新包
- **list** — 列出已安装的包
- **search** — 在注册表中搜索包

## 示例参数

```json
{ "action": "install", "package_name": "com.unity.collab-proxy", "version": "2.0.0" }
```

## 响应

成功: `{ "success": true, "data": ... }`. 失败: `{ "success": false, "message": "...", "error": "..." }`.
