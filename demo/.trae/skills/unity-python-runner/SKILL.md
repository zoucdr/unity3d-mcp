---
name: unity-python-runner
description: 在 Unity 环境中执行 Python 脚本。
---

# Unity MCP: python_runner

通过 `args` 传递参数。

## 参数 (args)

| 参数           | 类型     | 必填 | 说明 |
|---------------|----------|------|------|
| `action`     | string   | 是   | 操作: `run`, `execute_file`, `eval` |
| `script`     | string   | 是   | 要执行的 Python 脚本 |
| `file_path` | string   | 否   | 要执行的 Python 文件路径 |
| `args`      | object   | 否   | 传递给脚本的参数 |
| `timeout`   | integer  | 否   | 超时时间(秒，默认: 60) |
| `working_dir`| string   | 否   | 工作目录 |

- **run** — 内联运行 Python 脚本
- **execute_file** — 执行 Python 文件
- **eval** — 快速 Python 表达式求值

## 示例参数

```json
{ "action": "run", "script": "print('Hello from Python!')" }
```

## 响应

成功: `{ "success": true, "data": ... }`. 失败: `{ "success": false, "message": "...", "error": "..." }`.
