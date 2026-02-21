---
name: unity-base-editor
description: 管理 Unity 编辑器状态和控制。
---

# Unity MCP: base_editor

通过 `args` 传递参数。

## 参数 (args)

| 参数                  | 类型     | 必填 | 说明 |
|----------------------|----------|------|------|
| `action`             | string   | 是   | 操作类型: `get_state`, `get_windows`, `get_selection`, `execute_menu`, `get_menu_items` |
| `menu_path`          | string   | 否   | 菜单路径 (用于 execute_menu) |
| `root_path`          | string   | 否   | 根菜单路径 (用于 get_menu_items, 默认: "") |
| `include_submenus`   | boolean  | 否   | 包含子菜单 (用于 get_menu_items, 默认: false) |
| `verify_exists`      | boolean  | 否   | 验证菜单项是否存在 (用于 get_menu_items, 默认: false) |
| `wait_for_completion`| boolean  | 否   | 等待操作完成 (默认: true) |

- **get_state** — 获取当前 Unity 编辑器状态
- **get_windows** — 获取打开的编辑器窗口列表
- **get_selection** — 获取当前选中的对象
- **execute_menu** — 执行 Unity 菜单项
- **get_menu_items** — 获取可用的菜单项

## 示例参数

获取编辑器状态:
```json
{ "action": "get_state" }
```

执行菜单:
```json
{ "action": "execute_menu", "menu_path": "GameObject/3D Object/Cube" }
```

## 响应

成功: `{ "success": true, "data": ... }`. 失败: `{ "success": false, "message": "...", "error": "..." }`.
