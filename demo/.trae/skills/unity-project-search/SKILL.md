---
name: unity-project-search
description: 在项目窗口中搜索资源。
---

# Unity MCP: project_search

通过 `args` 传递参数。

## 参数 (args)

| 参数             | 类型     | 必填 | 说明 |
|-----------------|----------|------|------|
| `query`         | string   | 是   | 搜索关键词 |
| `search_target`| string   | 否   | 搜索类型: `asset`, `folder`, `script`, `texture`, `material`, `prefab`, `scene`, `audio`, `model` |
| `directory`     | string   | 否   | 搜索路径 (相对于 Assets) |
| `file_extension`| string   | 否   | 文件扩展名过滤器: `cs`, `mat`, `prefab`, `unity`, `png`, `jpg`, `fbx`, `wav`, `mp3` |
| `case_sensitive`| boolean  | 否   | 区分大小写 (默认: false) |
| `recursive`     | boolean  | 否   | 递归搜索子文件夹 (默认: true) |
| `include_meta`  | boolean  | 否   | 包含 .meta 文件 (默认: false) |
| `max_results`  | integer  | 否   | 最大结果数 (默认: 100) |

## 示例参数

```json
{ "query": "Player", "search_target": "prefab" }
```

## 响应

成功: `{ "success": true, "data": ... }`. 失败: `{ "success": false, "message": "...", "error": "..." }`.
