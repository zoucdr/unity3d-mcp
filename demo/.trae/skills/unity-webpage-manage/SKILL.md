---
name: unity-webpage-manage
description: 管理网页。
---

# Unity MCP: webpage_manage

通过 `args` 传递参数。

## 参数 (args)

| 参数          | 类型     | 必填 | 说明 |
|--------------|----------|------|------|
| `action`     | string   | 是   | 操作: `add`, `remove`, `update`, `search`, `categories`, `open`, `details` |
| `url`        | string   | 否   | 网站 URL |
| `description`| string   | 否   | 网站名称或说明 |
| `category`   | string   | 否   | 分类标签 (默认: "默认") |
| `id`         | integer  | 否   | 网页唯一ID (用于 remove/update) |
| `pattern`    | string   | 否   | 正则表达式 (用于搜索，匹配 URL、描述、分类、备注) |
| `note`       | string   | 否   | 备注信息 |

- **add** — 添加新网页
- **remove** — 删除网页
- **update** — 更新网页信息
- **search** — 按模式搜索网页
- **categories** — 列出所有分类
- **open** — 打开网页
- **details** — 获取网页详情

## 示例参数

```json
{ "action": "add", "url": "https://docs.unity.com", "description": "Unity 文档", "category": "文档" }
```

## 响应

成功: `{ "success": true, "data": ... }`. 失败: `{ "success": false, "message": "...", "error": "..." }`.
