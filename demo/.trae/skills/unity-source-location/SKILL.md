---
name: unity-source-location
description: 在文件浏览器或 Unity 项目中定位资源。
---

# Unity MCP: source_location

通过 `args` 传递参数。

## 参数 (args)

| 参数             | 类型     | 必填 | 说明 |
|-----------------|----------|------|------|
| `action`       | string   | 是   | 操作: `reveal`, `locate`, `find_references` |
| `target`       | string   | 是   | 资源或 GameObject 路径 |
| `open_explorer`| boolean  | 否   | 在文件浏览器中打开 (默认: true) |
| `focus_project`| boolean  | 否   | 聚焦 Unity 项目窗口 |

- **reveal** — 在文件浏览器中显示资源
- **locate** — 在 Unity 项目视图中定位
- **find_references** — 查找资源的所有引用

## 示例参数

```json
{ "action": "reveal", "target": "Assets/Textures/Player.png" }
```

## 响应

成功: `{ "success": true, "data": ... }`. 失败: `{ "success": false, "message": "...", "error": "..." }`.
