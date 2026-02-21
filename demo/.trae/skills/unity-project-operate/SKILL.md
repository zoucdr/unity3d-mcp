---
name: unity-project-operate
description: 管理 Unity 资源。
---

# Unity MCP: project_operate

通过 `args` 传递参数。

## 参数 (args)

| 参数                | 类型     | 必填 | 说明 |
|-------------------|----------|------|------|
| `action`          | string   | 是   | 操作: `import`, `modify`, `move`, `duplicate`, `rename`, `get_info`, `create_folder`, `reload`, `select`, `ping`, `select_depends`, `select_usage`, `tree` |
| `path`           | string   | 是   | 资源路径 (Unity 标准格式) |
| `destination`   | string   | 否   | 目标路径 (用于移动/复制) |
| `force`          | boolean  | 否   | 强制操作 (覆盖现有) |
| `properties`    | object   | 否   | 资源属性字典 |
| `refresh_type`  | string   | 否   | 刷新类型: `all`, `assets`, `scripts` (默认: all) |
| `save_before_refresh` | boolean | 否 | 刷新前保存所有 (默认: true) |
| `include_indirect` | boolean | 否   | 包含间接依赖 (默认: false) |
| `max_results`   | integer  | 否   | 最大结果数 (默认: 100) |

- **import** — 导入资源
- **modify** — 修改资源属性
- **move** — 移动资源
- **duplicate** — 复制资源
- **rename** — 重命名资源
- **get_info** — 获取资源信息
- **create_folder** — 创建文件夹
- **reload** — 重新加载资源
- **select** — 选中资源
- **ping** — 高亮资源
- **select_depends** — 选中依赖项
- **select_usage** — 选中用法
- **tree** — 显示资源树

## 示例参数

```json
{ "action": "move", "path": "Assets/OldFolder/file.cs", "destination": "Assets/NewFolder/file.cs" }
```

## 响应

成功: `{ "success": true, "data": ... }`. 失败: `{ "success": false, "message": "...", "error": "..." }`.
