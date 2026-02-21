---
name: unity-ui-rule-manage
description: 管理 UI 制作方案和修改记录。
---

# Unity MCP: ui_rule_manage

通过 `args` 传递参数。

## 参数 (args)

| 参数                | 类型     | 必填 | 说明 |
|--------------------|----------|------|------|
| `action`          | string   | 是   | 操作: `record_modify`, `record_renames`, `get_renames`, `record_download_sprites`, `get_download_sprites` |
| `name`            | string   | 是   | UI 名称 |
| `modify_desc`    | string   | 否   | 修改描述 |
| `names_data`     | object   | 否   | 节点名称数据 |
| `properties`     | object   | 否   | 属性数据 |
| `sprites_data`   | object   | 否   | 精灵数据 |
| `save_path`      | string   | 否   | 保存路径 |
| `auto_load_sprites` | boolean | 否   | 自动加载精灵 |

- **record_modify** — 记录 UI 修改
- **record_renames** — 记录节点重命名
- **get_renames** — 获取重命名记录
- **record_download_sprites** — 记录下载的精灵
- **get_download_sprites** — 获取下载的精灵

## 示例参数

```json
{ "action": "record_modify", "name": "MainMenu", "modify_desc": "更新按钮位置" }
```

## 响应

成功: `{ "success": true, "data": ... }`. 失败: `{ "success": false, "message": "...", "error": "..." }`.
