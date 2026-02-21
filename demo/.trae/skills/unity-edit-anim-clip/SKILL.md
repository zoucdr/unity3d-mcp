---
name: unity-edit-anim-clip
description: 管理动画片段。
---

# Unity MCP: edit_anim_clip

通过 `args` 传递参数。

## 参数 (args)

| 参数                           | 类型     | 必填 | 说明 |
|--------------------------------|----------|------|------|
| `action`                      | string   | 是   | 操作: `create`, `modify`, `duplicate`, `delete`, `get_info`, `search`, `set_curve`, `set_events` |
| `path`                        | string   | 否   | 动画片段资源路径 |
| `query`                       | string   | 否   | 搜索查询 (用于 search) |
| `recursive`                   | boolean  | 否   | 递归搜索 (默认: false) |
| `source_path`                 | string   | 否   | 源动画路径 (用于 duplicate) |
| `destination`                 | string   | 否   | 目标路径 |
| `force`                       | boolean  | 否   | 强制执行 (默认: false) |
| `length`                      | number   | 否   | 动画长度 (秒) |
| `loop_time`                   | boolean  | 否   | 循环播放 |
| `loop_pose`                   | boolean  | 否   | 循环姿势 |
| `frame_rate`                  | number   | 否   | 帧率 |
| `mirror`                      | boolean  | 否   | 镜像动画 |
| `cycle_offset`                | number   | 否   | 循环偏移 |
| `body_orientation`            | number   | 否   | 身体方向 |
| `height_from_ground`          | boolean  | 否   | 从地面计算高度 |
| `lock_root_height_y`          | boolean  | 否   | 锁定根高度 Y |
| `lock_root_rotation_y`       | boolean  | 否   | 锁定根旋转 Y |
| `lock_root_rotation_offset_y` | boolean  | 否   | 锁定根旋转偏移 Y |
| `root_height_offset_y`       | number   | 否   | 根高度 Y 偏移 |
| `root_height_offset_y_active` | boolean  | 否   | 启用根高度偏移 |
| `root_rotation_offset_y`     | number   | 否   | 根旋转 Y 偏移 |
| `keep_original_orientation_y` | boolean  | 否   | 保持原始方向 Y |
| `curves`                      | object   | 否   | 动画曲线数据 |
| `events`                      | array    | 否   | 动画事件 |

## 示例参数

创建动画:
```json
{ "action": "create", "path": "Assets/Animations/NewAnim.anim", "length": 2.0, "loop_time": true }
```

## 响应

成功: `{ "success": true, "data": ... }`. 失败: `{ "success": false, "message": "...", "error": "..." }`.
