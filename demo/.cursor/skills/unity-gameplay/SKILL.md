---
name: unity-gameplay
description: Control gameplay, simulate input, process images.
---

# Unity MCP: gameplay

Pass parameters in `args`.

## Parameters (args)

| Param           | Type     | Required | Description |
|-----------------|----------|----------|-------------|
| `action`        | string   | yes      | Operation: `play`, `pause`, `stop`, `screenshot`, `simulate_click`, `simulate_drag`, `set_size`, `get_info`, `compress_image` |
| `x`             | integer  | no       | X coordinate for input |
| `y`             | integer  | no       | Y coordinate for input |
| `target_x`      | integer  | no       | Target X (for drag) |
| `target_y`      | integer  | no       | Target Y (for drag) |
| `button`        | integer  | no       | Mouse button: 0=left, 1=right, 2=middle |
| `duration`      | number   | no       | Drag duration (seconds) |
| `delay`         | number   | no       | Delay before action (seconds) |
| `key_code`      | string   | no       | Keyboard key code |
| `delta`         | number   | no       | Scroll delta |
| `save_path`     | string   | no       | Screenshot save path |
| `region_x`      | integer  | no       | Screenshot region X |
| `region_y`      | integer  | no       | Screenshot region Y |
| `region_width`  | integer  | no       | Screenshot region width |
| `region_height` | integer  | no       | Screenshot region height |
| `width`         | integer  | no       | Game window width |
| `height`        | integer  | no       | Game window height |
| `size_name`     | string   | no       | Predefined size name |
| `format`        | string   | no       | Image format: `PNG`, `JPG` |
| `quality`       | integer  | no       | JPG quality (1-100) |
| `scale`         | number   | no       | Image scale factor |
| `source_path`   | string   | no       | Source image path (for compress) |
| `compress_ratio`| number   | no       | Compression ratio (0.1-1.0) |
| `count`         | integer  | no       | Batch operation count |
| `interval`      | number   | no       | Batch operation interval |
| `base_path`     | string   | no       | Base path for batch |

- **play** — Start playing the game
- **pause** — Pause the game
- **stop** — Stop the game
- **screenshot** — Take a screenshot
- **simulate_click** — Simulate mouse click
- **simulate_drag** — Simulate mouse drag
- **set_size** — Set game window size
- **get_info** — Get game info
- **compress_image** — Compress an image

## Example args

```json
{ "action": "play" }
```

```json
{ "action": "screenshot", "save_path": "Assets/Screenshots/screen.png" }
```

## Response

Success: `{ "success": true, "data": ... }`. Failure: `{ "success": false, "message": "...", "error": "..." }`.
