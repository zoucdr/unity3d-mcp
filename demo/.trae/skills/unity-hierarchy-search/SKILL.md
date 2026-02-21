---
name: unity-hierarchy-search
description: 在场景层级中搜索和查找游戏对象。
---

# Unity MCP: hierarchy_search

通过 `args` 传递参数。

## 参数 (args)

| 参数               | 类型     | 必填 | 说明 |
|-------------------|----------|------|------|
| `search_type`     | string   | 是   | 搜索方法: `by_name`, `by_id`, `by_tag`, `by_layer`, `by_component`, `by_query` |
| `query`           | string   | 是   | 搜索条件 (ID、名称或路径，支持通配符 *) |
| `include_inactive`| boolean  | 否   | 包含非激活对象 (默认: false) |
| `include_hierarchy`| boolean | 否   | 包含完整层级数据 (默认: false) |
| `select_many`     | boolean  | 否   | 查找所有匹配项 (默认: false) |
| `use_regex`       | boolean  | 否   | 使用正则表达式 (默认: false) |

- **by_name** — 按对象名称搜索
- **by_id** — 按实例ID搜索
- **by_tag** — 按标签搜索
- **by_layer** — 按层搜索
- **by_component** — 按组件搜索
- **by_query** — 基于查询的搜索

## 示例参数

```json
{ "search_type": "by_name", "query": "Player*" }
```

## 响应

成功: `{ "success": true, "data": ... }`. 失败: `{ "success": false, "message": "...", "error": "..." }`.
