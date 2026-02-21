---
name: unity-base-editor
description: Manage Unity editor state and control.
---

# Unity MCP: base_editor

Pass parameters in `args`.

## Parameters (args)

| Param             | Type    | Required | Description |
|-------------------|---------|----------|-------------|
| `action`          | string  | yes      | Operation type: `get_state`, `get_windows`, `get_selection`, `execute_menu`, `get_menu_items` |
| `menu_path`       | string  | no       | Menu path (for execute_menu) |
| `root_path`       | string  | no       | Root menu path (for get_menu_items, default: "") |
| `include_submenus`| boolean | no       | Include submenus (for get_menu_items, default: false) |
| `verify_exists`   | boolean | no       | Verify menu item exists (for get_menu_items, default: false) |
| `wait_for_completion` | boolean | no   | Wait for operation to complete (default: true) |

- **get_state** — Get current Unity editor state
- **get_windows** — Get list of open editor windows
- **get_selection** — Get currently selected objects
- **execute_menu** — Execute a Unity menu item
- **get_menu_items** — Get available menu items

## Example args

Get editor state:
```json
{ "action": "get_state" }
```

Execute menu:
```json
{ "action": "execute_menu", "menu_path": "GameObject/3D Object/Cube" }
```

## Response

Success: `{ "success": true, "data": ... }`. Failure: `{ "success": false, "message": "...", "error": "..." }`.
