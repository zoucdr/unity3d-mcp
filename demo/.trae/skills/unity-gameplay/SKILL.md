---
name: unity-gameplay
description: 控制游戏玩法、模拟输入、处理图像。
---

# Unity MCP: gameplay

通过 `args` 传递参数。

## 参数 (args)

| 参数             | 类型     | 必填 | 说明 |
|-----------------|----------|------|------|
| `action`        | string   | 是   | 操作: `play`, `pause`, `stop`, `screenshot`, `simulate_click`, `simulate_drag`, `set_size`, `get_info`, `compress_image` |
| `x`            | integer  | 否   | 输入 X 坐标 |
| `y`            | integer  | 否   | 输入 Y 坐标 |
| `target_x`     | integer  | 否   | 目标 X (用于拖动) |
| `target_y`     | integer  | 否   | 目标 Y (用于拖动) |
| `button`        | integer  | 否   | 鼠标按钮: 0=左, 1=右, 2=中 |
| `duration`      | number   | 否   | 拖动持续时间 (秒) |
| `delay`         | number   | 否   | 操作前延迟 (秒) |
| `key_code`      | string   | 否   | 键盘按键代码 |
| `delta`         | number   | 否   | 滚轮滚动量 |
| `save_path`     | string   | 否   | 截图保存路径 |
| `region_x`      | integer  | 否   | 截图区域 X |
| `region_y`      | integer  | 否   | 截图区域 Y |
| `region_width`  | integer  | 否   | 截图区域宽度 |
| `region_height` | integer  | 否   | 截图区域高度 |
| `width`         | integer  | 否   | 游戏窗口宽度 |
| `height`        | integer  | 否   | 游戏窗口高度 |
| `size_name`     | string   | 否   | 预定义尺寸名称 |
| `format`        | string   | 否   | 图片格式: `PNG`, `JPG` |
| `quality`       | integer  | 否   | JPG 质量 (1-100) |
| `scale`         | number   | 否   | 图片缩放比例 |
| `source_path`   | string   | 否   | 源图片路径 (用于压缩) |
| `compress_ratio`| number   | 否   | 压缩比 (0.1-1.0) |
| `count`         | integer  | 否   | 批量操作次数 |
| `interval`      | number   | 否   | 批量操作间隔 |
| `base_path`     | string   | 否   | 批量操作基础路径 |

- **play** — 开始播放游戏
- **pause** — 暂停游戏
- **stop** — 停止游戏
- **screenshot** — 截图
- **simulate_click** — 模拟鼠标点击
- **simulate_drag** — 模拟鼠标拖动
- **set_size** — 设置游戏窗口大小
- **get_info** — 获取游戏信息
- **compress_image** — 压缩图片

## 示例参数

```json
{ "action": "play" }
```

```json
{ "action": "screenshot", "save_path": "Assets/Screenshots/screen.png" }
```

## 响应

成功: `{ "success": true, "data": ... }`. 失败: `{ "success": false, "message": "...", "error": "..." }`.
